//
// This class is based heavily on the work of the OpenCover
// team in OpenCover.Framework.Symbols.CecilSymbolManager
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Extensions;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Coverlet.Core.Symbols
{
    internal class CecilSymbolHelper : ICecilSymbolHelper
    {
        private const int StepOverLineCode = 0xFEEFEE;
        // Create single instance, we cannot collide because we use full method name as key
        private readonly ConcurrentDictionary<string, int[]> _compilerGeneratedBranchesToExclude = new ConcurrentDictionary<string, int[]>();
        private readonly ConcurrentDictionary<string, List<int>> _sequencePointOffsetToSkip = new ConcurrentDictionary<string, List<int>>();

        // In case of nested compiler generated classes, only the root one presents the CompilerGenerated attribute.
        // So let's search up to the outermost declaring type to find the attribute
        private static bool IsCompilerGenerated(MethodDefinition methodDefinition)
        {
            TypeDefinition declaringType = methodDefinition.DeclaringType;
            while (declaringType != null)
            {
                if (declaringType.CustomAttributes.Any(ca => ca.AttributeType.FullName == typeof(CompilerGeneratedAttribute).FullName))
                {
                    return true;
                }
                declaringType = declaringType.DeclaringType;
            }

            return false;
        }

        private static bool IsCompilerGenerated(FieldDefinition fieldDefinition)
        {
            return fieldDefinition.DeclaringType.CustomAttributes.Any(ca => ca.AttributeType.FullName == typeof(CompilerGeneratedAttribute).FullName);
        }

        private static bool IsMoveNextInsideAsyncStateMachine(MethodDefinition methodDefinition)
        {
            if (methodDefinition.FullName.EndsWith("::MoveNext()") && IsCompilerGenerated(methodDefinition))
            {
                foreach (InterfaceImplementation implementedInterface in methodDefinition.DeclaringType.Interfaces)
                {
                    if (implementedInterface.InterfaceType.FullName == "System.Runtime.CompilerServices.IAsyncStateMachine")
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsMoveNextInsideEnumerator(MethodDefinition methodDefinition)
        {
            if (!methodDefinition.FullName.EndsWith("::MoveNext()"))
            {
                return false;
            }
            if (methodDefinition.DeclaringType.CustomAttributes.Count(ca => ca.AttributeType.FullName == typeof(CompilerGeneratedAttribute).FullName) > 0)
            {
                foreach (InterfaceImplementation implementedInterface in methodDefinition.DeclaringType.Interfaces)
                {
                    if (implementedInterface.InterfaceType.FullName == "System.Collections.IEnumerator")
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsMoveNextInsideAsyncStateMachineProlog(MethodDefinition methodDefinition)
        {
            /*
                int num = <>1__state;
                IL_0000: ldarg.0
                IL_0001: ldfld ...::'<>1__state'
                IL_0006: stloc.0
            */

            return (methodDefinition.Body.Instructions[0].OpCode == OpCodes.Ldarg_0 ||
                    methodDefinition.Body.Instructions[0].OpCode == OpCodes.Ldarg) &&

                    methodDefinition.Body.Instructions[1].OpCode == OpCodes.Ldfld &&
                    ((methodDefinition.Body.Instructions[1].Operand is FieldDefinition fd && fd.Name == "<>1__state") ||
                    (methodDefinition.Body.Instructions[1].Operand is FieldReference fr && fr.Name == "<>1__state")) &&

                    (methodDefinition.Body.Instructions[2].OpCode == OpCodes.Stloc &&
                    methodDefinition.Body.Instructions[2].Operand is VariableDefinition vd && vd.Index == 0) ||
                    methodDefinition.Body.Instructions[2].OpCode == OpCodes.Stloc_0;
        }

        private static bool SkipMoveNextPrologueBranches(Instruction instruction)
        {
            /*
                   If method is a generated MoveNext we'll skip first branches (could be a switch or a series of branches) 
                   that check state machine value to jump to correct state (for instance after a true async call)
                   Check if it's a Cond_Branch on state machine current value int num = <>1__state;
                   We are on branch OpCode so we need to go back by max 2 operation to reach ldloc.0 the load of "num"
                   Max 2 because we handle following patterns

                    Swich

                    // switch (num)
                    IL_0007: ldloc.0                        2
                    // (no C# code)
                    IL_0008: switch (IL_0037, IL_003c, ...  1
                    ...

                    Single branch

                    // if (num != 0)
                    IL_0007: ldloc.0           2
                    // (no C# code)
                    IL_0008: brfalse.s IL_000c 1
                    IL_000a: br.s IL_000e
                    IL_000c: br.s IL_0049
                    IL_000e: nop
                    ...

                    More tha one branch

                    // if (num != 0)
                    IL_0007: ldloc.0
                    // (no C# code)
                    IL_0008: brfalse.s IL_0012
                    IL_000a: br.s IL_000c
                    // if (num == 1)
                    IL_000c: ldloc.0       3
                    IL_000d: ldc.i4.1      2
                    IL_000e: beq.s IL_0014 1
                    // (no C# code)
                    IL_0010: br.s IL_0019
                    IL_0012: br.s IL_0060
                    IL_0014: br IL_00e5
                    IL_0019: nop
                    ...

                    so we know that current branch are checking that field and we're not interested in.
            */

            Instruction current = instruction.Previous;
            for (int instructionBefore = 2; instructionBefore > 0 && current.Previous != null; current = current.Previous, instructionBefore--)
            {
                if (
                        (current.OpCode == OpCodes.Ldloc && current.Operand is VariableDefinition vo && vo.Index == 0) ||
                        current.OpCode == OpCodes.Ldloc_0
                    )
                {
                    return true;
                }
            }
            return false;
        }

        private static bool SkipIsCompleteAwaiters(Instruction instruction)
        {
            // Skip get_IsCompleted to avoid unuseful branch due to async/await state machine
            if (
                    instruction.Previous.Operand is MethodReference operand &&
                    operand.Name == "get_IsCompleted" &&
                    (
                        operand.DeclaringType.FullName.StartsWith("System.Runtime.CompilerServices.TaskAwaiter") ||
                        operand.DeclaringType.FullName.StartsWith("System.Runtime.CompilerServices.ValueTaskAwaiter") ||
                        operand.DeclaringType.FullName.StartsWith("System.Runtime.CompilerServices.ConfiguredTaskAwaitable") ||
                        operand.DeclaringType.FullName.StartsWith("System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable")
                    )
                    &&
                    (
                        operand.DeclaringType.Scope.Name == "System.Runtime" ||
                        operand.DeclaringType.Scope.Name == "netstandard" ||
                        operand.DeclaringType.Scope.Name == "System.Threading.Tasks.Extensions"
                    )
               )
            {
                return true;
            }
            return false;
        }

        private static bool SkipLambdaCachedField(Instruction instruction)
        {
            /*
                Lambda cached field pattern

                IL_0074: ldloca.s 1
                IL_0076: call instance void [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter::GetResult()
                IL_007b: nop
                IL_007c: ldarg.0
                IL_007d: ldarg.0
                IL_007e: ldfld class [System.Runtime]System.Collections.Generic.IEnumerable`1<object> Coverlet.Core.Samples.Tests.Issue_730/'<DoSomethingAsyncWithLinq>d__1'::objects
                IL_0083: ldsfld class [System.Runtime]System.Func`2<object, object> Coverlet.Core.Samples.Tests.Issue_730/'<>c'::'<>9__1_0'
                IL_0088: dup
                IL_0089: brtrue.s IL_00a2 -> CHECK IF CACHED FIELD IS NULL OR JUMP TO DELEGATE USAGE

                (INIT STATIC FIELD)
                IL_008b: pop
                IL_008c: ldsfld class Coverlet.Core.Samples.Tests.Issue_730/'<>c' Coverlet.Core.Samples.Tests.Issue_730/'<>c'::'<>9'
                IL_0091: ldftn instance object Coverlet.Core.Samples.Tests.Issue_730/'<>c'::'<DoSomethingAsyncWithLinq>b__1_0'(object)
                IL_0097: newobj instance void class [System.Runtime]System.Func`2<object, object>::.ctor(object, native int)
                IL_009c: dup
                IL_009d: stsfld class [System.Runtime]System.Func`2<object, object> Coverlet.Core.Samples.Tests.Issue_730/'<>c'::'<>9__1_0'

                (USE DELEGATE FIELD)
                IL_00a2: call class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!1> [System.Linq]System.Linq.Enumerable::Select<object, object>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>, class [System.Runtime]System.Func`2<!!0, !!1>)
             */

            Instruction current = instruction.Previous;
            for (int instructionBefore = 2; instructionBefore > 0 && current.Previous != null; current = current.Previous, instructionBefore--)
            {
                if (current.OpCode == OpCodes.Ldsfld && current.Operand is FieldDefinition fd &&
                    // LambdaCacheField  https://github.com/dotnet/roslyn/blob/e704ca635bd6de70a0250e34c4567c7a28fa9f6d/src/Compilers/CSharp/Portable/Symbols/Synthesized/GeneratedNameKind.cs#L31
                    // https://github.com/dotnet/roslyn/blob/master/src/Compilers/CSharp/Portable/Symbols/Synthesized/GeneratedNames.cs#L145
                    fd.Name.StartsWith("<>9_") &&
                    IsCompilerGenerated(fd))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool SkipGeneratedBranchForExceptionRethrown(List<Instruction> instructions, Instruction instruction)
        {
            /* 
                In case of exception re-thrown inside the catch block, 
                the compiler generates a branch to check if the exception reference is null.

                A sample of generated code:

                IL_00b4: isinst [System.Runtime]System.Exception
                IL_00b9: stloc.s 6
                // if (ex == null)
                IL_00bb: ldloc.s 6
                // (no C# code)
                IL_00bd: brtrue.s IL_00c6

                So we can go back to previous instructions and skip this branch if recognize that type of code block
            */
            int branchIndex = instructions.BinarySearch(instruction, new InstructionByOffsetComparer());
            return branchIndex >= 3 && // avoid out of range exception (need almost 3 instruction before the branch)
                    instructions[branchIndex - 3].OpCode == OpCodes.Isinst &&
                    instructions[branchIndex - 3].Operand is TypeReference tr && tr.FullName == "System.Exception" &&
                    instructions[branchIndex - 2].OpCode == OpCodes.Stloc &&
                    instructions[branchIndex - 1].OpCode == OpCodes.Ldloc &&
                    // check for throw opcode after branch
                    instructions.Count - branchIndex >= 3 &&
                    instructions[branchIndex + 1].OpCode == OpCodes.Ldarg &&
                    instructions[branchIndex + 2].OpCode == OpCodes.Ldfld &&
                    instructions[branchIndex + 3].OpCode == OpCodes.Throw;
        }

        private bool SkipGeneratedBranchesForExceptionHandlers(MethodDefinition methodDefinition, Instruction instruction, List<Instruction> bodyInstructions)
        {
            if (!_compilerGeneratedBranchesToExclude.ContainsKey(methodDefinition.FullName))
            {
                /*
                  This method is used to parse compiler generated code inside async state machine and find branches generated for exception catch blocks
                  Typical generated code for catch block is:

                  catch ...
                  {
                      // (no C# code)
                      IL_0028: stloc.2
                      // object obj2 = <>s__1 = obj;
                      IL_0029: ldarg.0
                      // (no C# code)
                      IL_002a: ldloc.2
                      IL_002b: stfld object ...::'<>s__1'
                      // <>s__2 = 1;
                      IL_0030: ldarg.0
                      IL_0031: ldc.i4.1
                      IL_0032: stfld int32 ...::'<>s__2'      <- store 1 into <>s__2
                      // (no C# code)
                      IL_0037: leave.s IL_0039
                  } // end handle

                  // int num2 = <>s__2;
                  IL_0039: ldarg.0
                  IL_003a: ldfld int32 ...::'<>s__2'          <- load <>s__2 value and check if 1
                  IL_003f: stloc.3
                  // if (num2 == 1)
                  IL_0040: ldloc.3
                  IL_0041: ldc.i4.1
                  IL_0042: beq.s IL_0049                      <- BRANCH : if <>s__2 value is 1 go to exception handler code

                  IL_0044: br IL_00d6                         

                  IL_0049: nop                                <- start exception handler code

                  In case of multiple catch blocks as
                  try
                  {
                  }
                  catch (ExceptionType1)
                  {
                  }
                  catch (ExceptionType2)
                  {
                  }

                  generated IL contains multiple branches:
                  catch ...(type1)
                  {
                      ...
                  }
                  catch ...(type2)
                  {
                      ...
                  }
                  // int num2 = <>s__2;
                  IL_0039: ldarg.0
                  IL_003a: ldfld int32 ...::'<>s__2'          <- load <>s__2 value and check if 1
                  IL_003f: stloc.3
                  // if (num2 == 1)
                  IL_0040: ldloc.3
                  IL_0041: ldc.i4.1
                  IL_0042: beq.s IL_0049                      <- BRANCH 1 (type 1)

                  IL_0044: br IL_00d6                         

                  // if (num2 == 2)
                  IL_0067: ldloc.s 4
                  IL_0069: ldc.i4.2
                  IL_006a: beq IL_0104                        <- BRANCH 2 (type 2)

                  // (no C# code)
                  IL_006f: br IL_0191
               */
                List<int> detectedBranches = new List<int>();
                Collection<ExceptionHandler> handlers = methodDefinition.Body.ExceptionHandlers;

                int numberOfCatchBlocks = 1;
                foreach (var handler in handlers)
                {
                    if (handlers.Any(h => h.HandlerStart == handler.HandlerEnd))
                    {
                        // In case of multiple consecutive catch block
                        numberOfCatchBlocks++;
                        continue;
                    }

                    int currentIndex = bodyInstructions.BinarySearch(handler.HandlerEnd, new InstructionByOffsetComparer());

                    /* Detect flag load
                        // int num2 = <>s__2;
                        IL_0058: ldarg.0
                        IL_0059: ldfld int32 ...::'<>s__2'
                        IL_005e: stloc.s 4
                    */
                    if (bodyInstructions.Count - currentIndex > 3 && // check boundary
                        bodyInstructions[currentIndex].OpCode == OpCodes.Ldarg &&
                        bodyInstructions[currentIndex + 1].OpCode == OpCodes.Ldfld && bodyInstructions[currentIndex + 1].Operand is FieldReference fr && fr.Name.StartsWith("<>s__") &&
                        bodyInstructions[currentIndex + 2].OpCode == OpCodes.Stloc)
                    {
                        currentIndex += 3;
                        for (int i = 0; i < numberOfCatchBlocks; i++)
                        {
                            /*
                                // if (num2 == 1)
                                IL_0060: ldloc.s 4
                                IL_0062: ldc.i4.1
                                IL_0063: beq.s IL_0074

                                // (no C# code)
                                IL_0065: br.s IL_0067
                            */
                            if (bodyInstructions.Count - currentIndex > 4 && // check boundary
                                bodyInstructions[currentIndex].OpCode == OpCodes.Ldloc &&
                                bodyInstructions[currentIndex + 1].OpCode == OpCodes.Ldc_I4 &&
                                bodyInstructions[currentIndex + 2].OpCode == OpCodes.Beq &&
                                bodyInstructions[currentIndex + 3].OpCode == OpCodes.Br)
                            {
                                detectedBranches.Add(bodyInstructions[currentIndex + 2].Offset);
                            }
                            currentIndex += 4;
                        }
                    }
                }

                _compilerGeneratedBranchesToExclude.TryAdd(methodDefinition.FullName, detectedBranches.ToArray());
            }

            return _compilerGeneratedBranchesToExclude[methodDefinition.FullName].Contains(instruction.Offset);
        }

        // https://github.com/dotnet/roslyn/blob/master/docs/compilers/CSharp/Expression%20Breakpoints.md
        private bool SkipExpressionBreakpointsBranches(Instruction instruction) => instruction.Previous is not null && instruction.Previous.OpCode == OpCodes.Ldc_I4 &&
                                                                                    instruction.Previous.Operand is int operandValue && operandValue == 1 &&
                                                                                    instruction.Next is not null && instruction.Next.OpCode == OpCodes.Nop &&
                                                                                    instruction.Operand == instruction.Next?.Next;

        public IReadOnlyList<BranchPoint> GetBranchPoints(MethodDefinition methodDefinition)
        {
            var list = new List<BranchPoint>();
            if (methodDefinition is null)
            {
                return list;
            }

            uint ordinal = 0;
            var instructions = methodDefinition.Body.Instructions.ToList();

            bool isAsyncStateMachineMoveNext = IsMoveNextInsideAsyncStateMachine(methodDefinition);
            bool isMoveNextInsideAsyncStateMachineProlog = isAsyncStateMachineMoveNext && IsMoveNextInsideAsyncStateMachineProlog(methodDefinition);

            // State machine for enumerator uses `brfalse.s`/`beq` or `switch` opcode depending on how many `yield` we have in the method body.
            // For more than one `yield` a `switch` is emitted so we should only skip the first branch. In case of a single `yield` we need to
            // skip the first two branches to avoid reporting a phantom branch. The first branch (`brfalse.s`) jumps to the `yield`ed value,
            // the second one (`beq`) exits the enumeration.
            bool skipFirstBranch = IsMoveNextInsideEnumerator(methodDefinition);
            bool skipSecondBranch = false;

            foreach (Instruction instruction in instructions.Where(instruction => instruction.OpCode.FlowControl == FlowControl.Cond_Branch))
            {
                try
                {
                    if (skipFirstBranch)
                    {
                        skipFirstBranch = false;
                        skipSecondBranch = instruction.OpCode.Code != Code.Switch;
                        continue;
                    }

                    if (skipSecondBranch)
                    {
                        skipSecondBranch = false;
                        continue;
                    }

                    if (isMoveNextInsideAsyncStateMachineProlog)
                    {
                        if (SkipMoveNextPrologueBranches(instruction) || SkipIsCompleteAwaiters(instruction))
                        {
                            continue;
                        }
                    }

                    if (SkipExpressionBreakpointsBranches(instruction))
                    {
                        continue;
                    }

                    if (SkipLambdaCachedField(instruction))
                    {
                        continue;
                    }

                    if (isAsyncStateMachineMoveNext)
                    {
                        if (SkipGeneratedBranchesForExceptionHandlers(methodDefinition, instruction, instructions) ||
                            SkipGeneratedBranchForExceptionRethrown(instructions, instruction))
                        {
                            continue;
                        }
                    }
                    if (SkipBranchGeneratedExceptionFilter(instruction, methodDefinition))
                    {
                        continue;
                    }

                    if (SkipBranchGeneratedFinallyBlock(instruction, methodDefinition))
                    {
                        continue;
                    }

                    var pathCounter = 0;

                    // store branch origin offset
                    var branchOffset = instruction.Offset;
                    var closestSeqPt = FindClosestInstructionWithSequencePoint(methodDefinition.Body, instruction).Maybe(i => methodDefinition.DebugInformation.GetSequencePoint(i));
                    var branchingInstructionLine = closestSeqPt.Maybe(x => x.StartLine, -1);
                    var document = closestSeqPt.Maybe(x => x.Document.Url);

                    if (instruction.Next == null)
                    {
                        return list;
                    }

                    if (!BuildPointsForConditionalBranch(list, instruction, branchingInstructionLine, document, branchOffset, pathCounter, instructions, ref ordinal, methodDefinition))
                    {
                        return list;
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
            return list;
        }

        private static bool BuildPointsForConditionalBranch(List<BranchPoint> list, Instruction instruction,
            int branchingInstructionLine, string document, int branchOffset, int pathCounter,
            List<Instruction> instructions, ref uint ordinal, MethodDefinition methodDefinition)
        {
            // Add Default branch (Path=0)

            // Follow else/default instruction
            var @else = instruction.Next;

            var pathOffsetList = GetBranchPath(@else);

            // add Path 0
            var path0 = new BranchPoint
            {
                StartLine = branchingInstructionLine,
                Document = document,
                Offset = branchOffset,
                Ordinal = ordinal++,
                Path = pathCounter++,
                OffsetPoints =
                    pathOffsetList.Count > 1
                        ? pathOffsetList.GetRange(0, pathOffsetList.Count - 1)
                        : new List<int>(),
                EndOffset = pathOffsetList.Last()
            };

            // Add Conditional Branch (Path=1)
            if (instruction.OpCode.Code != Code.Switch)
            {
                // Follow instruction at operand
                var @then = instruction.Operand as Instruction;
                if (@then == null)
                    return false;

                ordinal = BuildPointsForBranch(list, then, branchingInstructionLine, document, branchOffset,
                    ordinal, pathCounter, path0, instructions, methodDefinition);
            }
            else // instruction.OpCode.Code == Code.Switch
            {
                var branchInstructions = instruction.Operand as Instruction[];
                if (branchInstructions == null || branchInstructions.Length == 0)
                    return false;

                ordinal = BuildPointsForSwitchCases(list, path0, branchInstructions, branchingInstructionLine,
                    document, branchOffset, ordinal, ref pathCounter);
            }
            return true;
        }

        private static uint BuildPointsForBranch(List<BranchPoint> list, Instruction then, int branchingInstructionLine, string document,
            int branchOffset, uint ordinal, int pathCounter, BranchPoint path0, List<Instruction> instructions, MethodDefinition methodDefinition)
        {
            var pathOffsetList1 = GetBranchPath(@then);

            // Add path 1
            var path1 = new BranchPoint
            {
                StartLine = branchingInstructionLine,
                Document = document,
                Offset = branchOffset,
                Ordinal = ordinal++,
                Path = pathCounter,
                OffsetPoints =
                    pathOffsetList1.Count > 1
                        ? pathOffsetList1.GetRange(0, pathOffsetList1.Count - 1)
                        : new List<int>(),
                EndOffset = pathOffsetList1.Last()
            };

            // only add branch if branch does not match a known sequence 
            // e.g. auto generated field assignment
            // or encapsulates at least one sequence point
            var offsets = new[]
            {
                path0.Offset,
                path0.EndOffset,
                path1.Offset,
                path1.EndOffset
            };

            var ignoreSequences = new[]
            {
                // we may need other samples
                new[] {Code.Brtrue_S, Code.Pop, Code.Ldsfld, Code.Ldftn, Code.Newobj, Code.Dup, Code.Stsfld, Code.Newobj}, // CachedAnonymousMethodDelegate field allocation 
            };

            var bs = offsets.Min();
            var be = offsets.Max();

            var range = instructions.Where(i => (i.Offset >= bs) && (i.Offset <= be)).ToList();

            var match = ignoreSequences
                .Where(ignoreSequence => range.Count >= ignoreSequence.Length)
                .Any(ignoreSequence => range.Zip(ignoreSequence, (instruction, code) => instruction.OpCode.Code == code).All(x => x));

            var count = range
                .Count(i => methodDefinition.DebugInformation.GetSequencePoint(i) != null);

            if (!match || count > 0)
            {
                list.Add(path0);
                list.Add(path1);
            }
            return ordinal;
        }

        private static uint BuildPointsForSwitchCases(List<BranchPoint> list, BranchPoint path0, Instruction[] branchInstructions,
            int branchingInstructionLine, string document, int branchOffset, uint ordinal, ref int pathCounter)
        {
            var counter = pathCounter;
            list.Add(path0);
            // Add Conditional Branches (Path>0)
            list.AddRange(branchInstructions.Select(GetBranchPath)
                .Select(pathOffsetList1 => new BranchPoint
                {
                    StartLine = branchingInstructionLine,
                    Document = document,
                    Offset = branchOffset,
                    Ordinal = ordinal++,
                    Path = counter++,
                    OffsetPoints =
                        pathOffsetList1.Count > 1
                            ? pathOffsetList1.GetRange(0, pathOffsetList1.Count - 1)
                            : new List<int>(),
                    EndOffset = pathOffsetList1.Last()
                }));
            pathCounter = counter;
            return ordinal;
        }

        public bool SkipNotCoverableInstruction(MethodDefinition methodDefinition, Instruction instruction) =>
            SkipNotCoverableInstructionAfterExceptionRethrowInsiceCatchBlock(methodDefinition, instruction) ||
            SkipExpressionBreakpointsSequences(methodDefinition, instruction);

        /*
           Need to skip instrumentation after exception re-throw inside catch block (only for async state machine MoveNext())
           es:
           try 
           {
               ...
           }
           catch
           {
               await ...
               throw; 
           } // need to skip instrumentation here

           We can detect this type of code block by searching for method ExceptionDispatchInfo.Throw() inside the compiled IL
           ...
           // ExceptionDispatchInfo.Capture(ex).Throw();
           IL_00c6: ldloc.s 6
           IL_00c8: call class [System.Runtime]System.Runtime.ExceptionServices.ExceptionDispatchInfo [System.Runtime]System.Runtime.ExceptionServices.ExceptionDispatchInfo::Capture(class [System.Runtime]System.Exception)
           IL_00cd: callvirt instance void [System.Runtime]System.Runtime.ExceptionServices.ExceptionDispatchInfo::Throw()
           // NOT COVERABLE
           IL_00d2: nop
           IL_00d3: nop
           ...

           In case of nested code blocks inside catch we need to detect also goto calls
           ...
           // ExceptionDispatchInfo.Capture(ex).Throw();
           IL_00d3: ldloc.s 7
           IL_00d5: call class [System.Runtime]System.Runtime.ExceptionServices.ExceptionDispatchInfo [System.Runtime]System.Runtime.ExceptionServices.ExceptionDispatchInfo::Capture(class [System.Runtime]System.Exception)
           IL_00da: callvirt instance void [System.Runtime]System.Runtime.ExceptionServices.ExceptionDispatchInfo::Throw()
           // NOT COVERABLE
           IL_00df: nop
           IL_00e0: nop
           IL_00e1: br.s IL_00ea
           ...
           // NOT COVERABLE
           IL_00ea: nop                
           IL_00eb: br.s IL_00ed
           ...
       */
        public bool SkipNotCoverableInstructionAfterExceptionRethrowInsiceCatchBlock(MethodDefinition methodDefinition, Instruction instruction)
        {
            if (!IsMoveNextInsideAsyncStateMachine(methodDefinition))
            {
                return false;
            }

            if (instruction.OpCode != OpCodes.Nop)
            {
                return false;
            }

            // detect if current instruction is not coverable
            Instruction prev = GetPreviousNoNopInstruction(instruction);
            if (prev != null &&
                prev.OpCode == OpCodes.Callvirt &&
                prev.Operand is MethodReference mr && mr.FullName == "System.Void System.Runtime.ExceptionServices.ExceptionDispatchInfo::Throw()")
            {
                return true;
            }

            // find the caller of current instruction and detect if not coverable
            prev = instruction.Previous;
            while (prev != null)
            {
                if (prev.Operand is Instruction i && (i.Offset == instruction.Offset || i.Offset == prev.Next.Offset)) // caller
                {
                    prev = GetPreviousNoNopInstruction(prev);
                    break;
                }
                prev = prev.Previous;
            }

            return prev != null &&
                prev.OpCode == OpCodes.Callvirt &&
                prev.Operand is MethodReference mr1 && mr1.FullName == "System.Void System.Runtime.ExceptionServices.ExceptionDispatchInfo::Throw()";

            // local helper
            static Instruction GetPreviousNoNopInstruction(Instruction i)
            {
                Instruction instruction = i.Previous;
                while (instruction != null)
                {
                    if (instruction.OpCode != OpCodes.Nop)
                    {
                        return instruction;
                    }
                    instruction = instruction.Previous;
                }

                return null;
            }
        }

        private bool SkipExpressionBreakpointsSequences(MethodDefinition methodDefinition, Instruction instruction)
        {
            if (_sequencePointOffsetToSkip.ContainsKey(methodDefinition.FullName) && _sequencePointOffsetToSkip[methodDefinition.FullName].Contains(instruction.Offset) && instruction.OpCode == OpCodes.Nop)
            {
                return true;
            }
            /* 
               Sequence to skip https://github.com/dotnet/roslyn/blob/master/docs/compilers/CSharp/Expression%20Breakpoints.md
               // if (1 == 0)
               // sequence point: (line 33, col 9) to (line 40, col 10) in C:\git\coverletfork\test\coverlet.core.tests\Samples\Instrumentation.SelectionStatements.cs
               IL_0000: ldc.i4.1
               IL_0001: brtrue.s IL_0004
               // if (value is int)
               // sequence point: (line 34, col 9) to (line 40, col 10) in C:\git\coverletfork\test\coverlet.core.tests\Samples\Instrumentation.SelectionStatements.cs
               IL_0003: nop
                // sequence point: hidden
                ...
             */
            if (
                    instruction.OpCode == OpCodes.Ldc_I4 && instruction.Operand is int operandValue && operandValue == 1 &&
                    instruction.Next?.OpCode == OpCodes.Brtrue &&
                    instruction.Next?.Next?.OpCode == OpCodes.Nop &&
                    instruction.Next?.Operand == instruction.Next?.Next?.Next &&
                    methodDefinition.DebugInformation.GetSequencePoint(instruction.Next?.Next) is not null
                )
            {
                if (!_sequencePointOffsetToSkip.ContainsKey(methodDefinition.FullName))
                {
                    _sequencePointOffsetToSkip.TryAdd(methodDefinition.FullName, new List<int>());
                }
                _sequencePointOffsetToSkip[methodDefinition.FullName].Add(instruction.Offset);
                _sequencePointOffsetToSkip[methodDefinition.FullName].Add(instruction.Next.Offset);
                _sequencePointOffsetToSkip[methodDefinition.FullName].Add(instruction.Next.Next.Offset);
            }

            return false;
        }

        private static bool SkipBranchGeneratedExceptionFilter(Instruction branchInstruction, MethodDefinition methodDefinition)
        {
            if (!methodDefinition.Body.HasExceptionHandlers)
                return false;

            // a generated filter block will have no sequence points in its range
            var handlers = methodDefinition.Body.ExceptionHandlers
                .Where(e => e.HandlerType == ExceptionHandlerType.Filter)
                .ToList();

            foreach (var exceptionHandler in handlers)
            {
                Instruction startFilter = exceptionHandler.FilterStart;
                Instruction endFilter = startFilter;

                while (endFilter != null && endFilter.OpCode != OpCodes.Endfilter)
                {
                    endFilter = endFilter.Next;
                }

                if (branchInstruction.Offset >= startFilter.Offset && branchInstruction.Offset <= endFilter.Offset)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SkipBranchGeneratedFinallyBlock(Instruction branchInstruction, MethodDefinition methodDefinition)
        {
            if (!methodDefinition.Body.HasExceptionHandlers)
                return false;

            // a generated finally block will have no sequence points in its range
            var handlers = methodDefinition.Body.ExceptionHandlers
                .Where(e => e.HandlerType == ExceptionHandlerType.Finally)
                .ToList();

            return handlers
                .Where(e => branchInstruction.Offset >= e.HandlerStart.Offset)
                .Where(e => branchInstruction.Offset < e.HandlerEnd.Maybe(h => h.Offset, GetOffsetOfNextEndfinally(methodDefinition.Body, e.HandlerStart.Offset)))
                .OrderByDescending(h => h.HandlerStart.Offset) // we need to work inside out
                .Any(eh => !(methodDefinition.DebugInformation.GetSequencePointMapping()
                    .Where(i => i.Value.StartLine != StepOverLineCode)
                    .Any(i => i.Value.Offset >= eh.HandlerStart.Offset && i.Value.Offset < eh.HandlerEnd.Maybe(h => h.Offset, GetOffsetOfNextEndfinally(methodDefinition.Body, eh.HandlerStart.Offset)))));
        }

        private static int GetOffsetOfNextEndfinally(MethodBody body, int startOffset)
        {
            var lastOffset = body.Instructions.LastOrDefault().Maybe(i => i.Offset, int.MaxValue);
            return body.Instructions.FirstOrDefault(i => i.Offset >= startOffset && i.OpCode.Code == Code.Endfinally).Maybe(i => i.Offset, lastOffset);
        }

        private static List<int> GetBranchPath(Instruction instruction)
        {
            var offsetList = new List<int>();

            if (instruction != null)
            {
                var point = instruction;
                offsetList.Add(point.Offset);
                while (point.OpCode == OpCodes.Br || point.OpCode == OpCodes.Br_S)
                {
                    var nextPoint = point.Operand as Instruction;
                    if (nextPoint != null)
                    {
                        point = nextPoint;
                        offsetList.Add(point.Offset);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return offsetList;
        }

        private static Instruction FindClosestInstructionWithSequencePoint(MethodBody methodBody, Instruction instruction)
        {
            var sequencePointsInMethod = methodBody.Instructions.Where(i => HasValidSequencePoint(i, methodBody.Method)).ToList();
            if (!sequencePointsInMethod.Any())
                return null;
            var idx = sequencePointsInMethod.BinarySearch(instruction, new InstructionByOffsetComparer());
            Instruction prev;
            if (idx < 0)
            {
                // no exact match, idx corresponds to the next, larger element
                var lower = Math.Max(~idx - 1, 0);
                prev = sequencePointsInMethod[lower];
            }
            else
            {
                // exact match, idx corresponds to the match
                prev = sequencePointsInMethod[idx];
            }

            return prev;
        }

        private static bool HasValidSequencePoint(Instruction instruction, MethodDefinition methodDefinition)
        {
            var sp = methodDefinition.DebugInformation.GetSequencePoint(instruction);
            return sp != null && sp.StartLine != StepOverLineCode;
        }

        private class InstructionByOffsetComparer : IComparer<Instruction>
        {
            public int Compare(Instruction x, Instruction y)
            {
                return x.Offset.CompareTo(y.Offset);
            }
        }
    }
}