//
// This class is based heavily on the work of the OpenCover
// team in OpenCover.Framework.Symbols.CecilSymbolManager
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Coverlet.Core.Extensions;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Coverlet.Core.Symbols
{
    internal static class CecilSymbolHelper
    {
        private const int StepOverLineCode = 0xFEEFEE;

        private static bool IsMoveNextInsideAsyncStateMachine(MethodDefinition methodDefinition)
        {
            if (!methodDefinition.FullName.EndsWith("::MoveNext()"))
            {
                return false;
            }

            if (methodDefinition.DeclaringType.CustomAttributes.Count(ca => ca.AttributeType.FullName == typeof(CompilerGeneratedAttribute).FullName) > 0)
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

        private static bool IsRecognizedMoveNextInsideAsyncStateMachineProlog(MethodDefinition methodDefinition)
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
                    methodDefinition.Body.Instructions[1].Operand is FieldDefinition fd && fd.Name == "<>1__state" &&

                    (methodDefinition.Body.Instructions[2].OpCode == OpCodes.Stloc &&
                    methodDefinition.Body.Instructions[2].Operand is VariableDefinition vd && vd.Index == 0) ||
                    methodDefinition.Body.Instructions[2].OpCode == OpCodes.Stloc_0;
        }

        public static List<BranchPoint> GetBranchPoints(MethodDefinition methodDefinition)
        {
            var list = new List<BranchPoint>();
            if (methodDefinition == null)
                return list;

            UInt32 ordinal = 0;
            var instructions = methodDefinition.Body.Instructions;

            bool isAsyncStateMachineMoveNext = IsMoveNextInsideAsyncStateMachine(methodDefinition);
            bool isRecognizedMoveNextInsideAsyncStateMachineProlog = isAsyncStateMachineMoveNext && IsRecognizedMoveNextInsideAsyncStateMachineProlog(methodDefinition);
            bool skipFirstBranch = IsMoveNextInsideEnumerator(methodDefinition);

            foreach (Instruction instruction in instructions.Where(instruction => instruction.OpCode.FlowControl == FlowControl.Cond_Branch))
            {
                try
                {
                    if (skipFirstBranch)
                    {
                        skipFirstBranch = false;
                        continue;
                    }

                    /* 
                       If method is a generated MoveNext we'll skip first branches (could be a switch or a series of branches) 
                       that check state machine value to jump to correct state(for instance after a true async call)
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
                    if (isRecognizedMoveNextInsideAsyncStateMachineProlog)
                    {
                        bool skipInstruction = false;
                        Instruction current = instruction.Previous;
                        for (int instructionBefore = 2; instructionBefore > 0 && current.Previous != null; current = current.Previous, instructionBefore--)
                        {
                            if (
                                    (current.OpCode == OpCodes.Ldloc && current.Operand is VariableDefinition vo && vo.Index == 0) ||
                                    current.OpCode == OpCodes.Ldloc_0
                                )
                            {
                                skipInstruction = true;
                                break;
                            }
                        }
                        if (skipInstruction)
                        {
                            continue;
                        }
                    }

                    // Skip get_IsCompleted to avoid unuseful branch due to async/await state machine
                    if (
                            isRecognizedMoveNextInsideAsyncStateMachineProlog && instruction.Previous.Operand is MethodReference operand &&
                            operand.Name == "get_IsCompleted" &&
                            (
                                operand.DeclaringType.FullName.StartsWith("System.Runtime.CompilerServices.TaskAwaiter") ||
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
                        continue;
                    }

                    if (BranchIsInGeneratedExceptionFilter(instruction, methodDefinition))
                        continue;

                    if (BranchIsInGeneratedFinallyBlock(instruction, methodDefinition))
                        continue;

                    var pathCounter = 0;

                    // store branch origin offset
                    var branchOffset = instruction.Offset;
                    var closestSeqPt = FindClosestInstructionWithSequencePoint(methodDefinition.Body, instruction).Maybe(i => methodDefinition.DebugInformation.GetSequencePoint(i));
                    var branchingInstructionLine = closestSeqPt.Maybe(x => x.StartLine, -1);
                    var document = closestSeqPt.Maybe(x => x.Document.Url);

                    if (instruction.Next == null)
                        return list;

                    if (!BuildPointsForConditionalBranch(list, instruction, branchingInstructionLine, document, branchOffset, pathCounter, instructions, ref ordinal, methodDefinition))
                        return list;
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
            Collection<Instruction> instructions, ref uint ordinal, MethodDefinition methodDefinition)
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
            int branchOffset, uint ordinal, int pathCounter, BranchPoint path0, Collection<Instruction> instructions, MethodDefinition methodDefinition)
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

        private static bool BranchIsInGeneratedExceptionFilter(Instruction branchInstruction, MethodDefinition methodDefinition)
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

                while (endFilter.OpCode != OpCodes.Endfilter && endFilter != null)
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

        private static bool BranchIsInGeneratedFinallyBlock(Instruction branchInstruction, MethodDefinition methodDefinition)
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