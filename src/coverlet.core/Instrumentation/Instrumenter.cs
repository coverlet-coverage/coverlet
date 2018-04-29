using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Coverlet.Core.Helpers;
using Coverlet.Core.Extensions;
using Coverlet.Core.Symbols;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;

using Newtonsoft.Json;

namespace Coverlet.Core.Instrumentation
{
    internal class Instrumenter
    {
        private const int StepOverLineCode = 0xFEEFEE;
        private readonly string _module;
        private readonly string _identifier;
        private IEnumerable<string> _excludedFiles;
        private InstrumenterResult _result;

        public Instrumenter(string module, string identifier, IEnumerable<string> excludedFiles = null)
        {
            _module = module;
            _identifier = identifier;
            _excludedFiles = excludedFiles ?? Enumerable.Empty<string>();
        }

        public bool CanInstrument() => InstrumentationHelper.HasPdb(_module);

        public InstrumenterResult Instrument()
        {
            string hitsFilePath = Path.Combine(
                Path.GetTempPath(),
                Path.GetFileNameWithoutExtension(_module) + "_" + _identifier
            );

            _result = new InstrumenterResult
            {
                Module = Path.GetFileNameWithoutExtension(_module),
                HitsFilePath = hitsFilePath,
                ModulePath = _module
            };

            InstrumentModule();
            InstrumentationHelper.CopyCoverletDependency(_module);

            return _result;
        }

        private void InstrumentModule()
        {
            using (var stream = new FileStream(_module, FileMode.Open, FileAccess.ReadWrite))
            using (var resolver = new DefaultAssemblyResolver())
            {
                resolver.AddSearchDirectory(Path.GetDirectoryName(_module));
                var parameters = new ReaderParameters { ReadSymbols = true, AssemblyResolver = resolver };
                using (var module = ModuleDefinition.ReadModule(stream, parameters))
                {
                    foreach (var type in module.GetTypes())
                    {
                        if (type.CustomAttributes.Any(a => a.AttributeType.Name == "ExcludeFromCoverageAttribute" || a.AttributeType.Name == "ExcludeFromCoverage"))
                            continue;

                        foreach (var method in type.Methods)
                        {
	                        var sourceFile = method.DebugInformation.SequencePoints.Select(s => s.Document.Url).FirstOrDefault();
	                        if (!string.IsNullOrEmpty(sourceFile) && _excludedFiles.Contains(sourceFile)) {
	                            continue;
	                        }
                            if (!method.CustomAttributes.Any(a => a.AttributeType.Name == "ExcludeFromCoverageAttribute" || a.AttributeType.Name == "ExcludeFromCoverage"))
                                InstrumentMethod(method);
                        }
                    }

                    module.Write(stream);
                }
            }
        }

        private void InstrumentMethod(MethodDefinition method)
        {
            if (!method.HasBody)
                return;

            InstrumentIL(method);
        }

        private void InstrumentIL(MethodDefinition method)
        {
            method.Body.SimplifyMacros();
            ILProcessor processor = method.Body.GetILProcessor();

            // Write All Instructions
            if(method.Name.Contains("CreateAddress"))
            {
                Console.WriteLine($"All Instructions: {processor.Body.Instructions.Count}");
                foreach (var instruction in processor.Body.Instructions)
                    Console.WriteLine($"{instruction} // Offset: {instruction.Offset}");
            }

            var index = 0;
            var count = processor.Body.Instructions.Count;
            var branchPoints = new List<BranchPoint>();
            uint ordinal = 0;

            // if method is a generated MoveNext skip first branch (could be a switch or a branch)
            var skipFirstBranch = IsMovenext.IsMatch(method.FullName);

            for (int n = 0; n < count; n++)
            {
                var instruction = processor.Body.Instructions[index];
                var sequencePoint = method.DebugInformation.GetSequencePoint(instruction);
                var isBranchPoint = instruction.OpCode.FlowControl == FlowControl.Cond_Branch;
                var targetedBranchPoints = branchPoints.Where(p => p.EndOffset == instruction.Offset);

                // Find Branch Targets
                if (isBranchPoint)
                {
                    if (skipFirstBranch)
                    {
                        skipFirstBranch = false;
                        index++;
                        continue;
                    }

                    if (BranchIsInGeneratedFinallyBlock(instruction, method))
                    {
                        index++;
                        continue;
                    }

                    var pathCounter = 0;

                    // store branch origin offset
                    var branchOffset = instruction.Offset;
                    var closestSeqPt = FindClosestInstructionWithSequencePoint(method.Body, instruction).Maybe(i => method.DebugInformation.GetSequencePoint(i));
                    var branchingInstructionLine = closestSeqPt.Maybe(x => x.StartLine, -1);
                    var document = closestSeqPt.Maybe(x => x.Document.Url);

                    if (null == instruction.Next)
                    {
                        index++;
                        continue;
                    }

                    if (!BuildPointsForConditionalBranch(branchPoints, instruction, branchingInstructionLine, document, branchOffset, pathCounter, processor.Body.Instructions, ref ordinal, method))
                    {
                        index++;
                        continue;
                    }
                }
                
                if (sequencePoint != null && !sequencePoint.IsHidden)
                {
                    var target = AddInstrumentationCode(method, processor, instruction, sequencePoint);
                    foreach (var _instruction in processor.Body.Instructions)
                        ReplaceInstructionTarget(_instruction, instruction, target);

                    foreach (ExceptionHandler handler in processor.Body.ExceptionHandlers)
                        ReplaceExceptionHandlerBoundary(handler, instruction, target);

                    index += 3;
                }

                if (targetedBranchPoints.Count() > 0)
                {
                    foreach (var _branchTarget in targetedBranchPoints)
                    {
                        var target = AddInstrumentationCode(method, processor, instruction, _branchTarget);
                        foreach (var _instruction in processor.Body.Instructions)
                            ReplaceInstructionTarget(_instruction, instruction, target);

                        foreach (ExceptionHandler handler in processor.Body.ExceptionHandlers)
                            ReplaceExceptionHandlerBoundary(handler, instruction, target);

                        index += 3;
                    }
                }

                index++;
            }

            // Write New Instructions
            if(method.Name.Contains("CreateAddress"))
            {
                Console.WriteLine($"New Instructions: {processor.Body.Instructions.Count}");
                foreach (var instruction in processor.Body.Instructions)
                    Console.WriteLine($"{instruction} // Offset: {instruction.Offset}");
            }

            // Write All Branches
            if(method.Name.Contains("CreateAddress"))
                Console.WriteLine(JsonConvert.SerializeObject(branchPoints));

            method.Body.OptimizeMacros();
        }

        private Instruction AddInstrumentationCode(MethodDefinition method, ILProcessor processor, Instruction instruction, SequencePoint sequencePoint)
        {
            var document = _result.Documents.FirstOrDefault(d => d.Path == sequencePoint.Document.Url);
            if (document == null)
            {
                document = new Document { Path = sequencePoint.Document.Url };
                _result.Documents.Add(document);
            }

            for (int i = sequencePoint.StartLine; i <= sequencePoint.EndLine; i++)
            {
                if (!document.Lines.Exists(l => l.Number == i))
                    document.Lines.Add(new Line { Number = i, Class = method.DeclaringType.FullName, Method = method.FullName });
            }

            // string flag = branchPoints.Count > 0 ? "B" : "L";
            string marker = $"{document.Path},{sequencePoint.StartLine},{sequencePoint.EndLine},L";

            var pathInstr = Instruction.Create(OpCodes.Ldstr, _result.HitsFilePath);
            var markInstr = Instruction.Create(OpCodes.Ldstr, marker);
            var callInstr = Instruction.Create(OpCodes.Call, processor.Body.Method.Module.ImportReference(typeof(CoverageTracker).GetMethod("MarkExecuted")));

            processor.InsertBefore(instruction, callInstr);
            processor.InsertBefore(callInstr, markInstr);
            processor.InsertBefore(markInstr, pathInstr);

            return pathInstr;
        }

        private Instruction AddInstrumentationCode(MethodDefinition method, ILProcessor processor, Instruction instruction, BranchPoint branchPoint)
        {
            var document = _result.Documents.FirstOrDefault(d => d.Path == branchPoint.Document);
            if (document == null)
            {
                document = new Document { Path = branchPoint.Document };
                _result.Documents.Add(document);
            }

            if (!document.Branches.Exists(l => l.Number == branchPoint.StartLine && l.Path == branchPoint.Path && l.Ordinal == branchPoint.Ordinal))
                document.Branches.Add(new Branch { Number = branchPoint.StartLine, Class = method.DeclaringType.FullName, Method = method.FullName, Path = branchPoint.Path, Ordinal = branchPoint.Ordinal });

            string marker = $"{document.Path},{branchPoint.StartLine},{branchPoint.StartLine},B,{branchPoint.Path},{branchPoint.Ordinal}";

            var pathInstr = Instruction.Create(OpCodes.Ldstr, _result.HitsFilePath);
            var markInstr = Instruction.Create(OpCodes.Ldstr, marker);
            var callInstr = Instruction.Create(OpCodes.Call, processor.Body.Method.Module.ImportReference(typeof(CoverageTracker).GetMethod("MarkExecuted")));

            processor.InsertBefore(instruction, callInstr);
            processor.InsertBefore(callInstr, markInstr);
            processor.InsertBefore(markInstr, pathInstr);

            return pathInstr;
        }

        private static void ReplaceInstructionTarget(Instruction instruction, Instruction oldTarget, Instruction newTarget)
        {
            if (instruction.Operand is Instruction _instruction)
            {
                if (_instruction == oldTarget)
                {
                    instruction.Operand = newTarget;
                    return;
                }
            }
            else if (instruction.Operand is Instruction[] _instructions)
            {
                for (int i = 0; i < _instructions.Length; i++)
                {
                    if (_instructions[i] == oldTarget)
                        _instructions[i] = newTarget;
                }
            }
        }

        private static void ReplaceExceptionHandlerBoundary(ExceptionHandler handler, Instruction oldTarget, Instruction newTarget)
        {
            if (handler.FilterStart == oldTarget)
                handler.FilterStart = newTarget;

            if (handler.HandlerEnd == oldTarget)
                handler.HandlerEnd = newTarget;

            if (handler.HandlerStart == oldTarget)
                handler.HandlerStart = newTarget;

            if (handler.TryEnd == oldTarget)
                handler.TryEnd = newTarget;

            if (handler.TryStart == oldTarget)
                handler.TryStart = newTarget;
        }

        private static readonly Regex IsMovenext = new Regex(@"\<[^\s>]+\>\w__\w(\w)?::MoveNext\(\)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private void GetBranchPoints(MethodDefinition methodDefinition, List<BranchPoint> list)
        {
            if (methodDefinition == null) 
                return;
            try
            {
                UInt32 ordinal = 0;
                var instructions = methodDefinition.Body.Instructions;
                
                // if method is a generated MoveNext skip first branch (could be a switch or a branch)
                var skipFirstBranch = IsMovenext.IsMatch(methodDefinition.FullName);

                foreach (var instruction in instructions.Where(instruction => instruction.OpCode.FlowControl == FlowControl.Cond_Branch))
                {
                    if (skipFirstBranch)
                    {
                        skipFirstBranch = false;
                        continue;
                    }

                    if (BranchIsInGeneratedFinallyBlock(instruction, methodDefinition)) 
                        continue;

                    var pathCounter = 0;

                    // store branch origin offset
                    var branchOffset = instruction.Offset;
                    var closestSeqPt = FindClosestInstructionWithSequencePoint(methodDefinition.Body, instruction).Maybe(i => methodDefinition.DebugInformation.GetSequencePoint(i));
                    var branchingInstructionLine = closestSeqPt.Maybe(x => x.StartLine, -1);
                    var document = closestSeqPt.Maybe(x => x.Document.Url);

                    if (null == instruction.Next)
                        return;

                    if (!BuildPointsForConditionalBranch(list, instruction, branchingInstructionLine, document, branchOffset, pathCounter, instructions, ref ordinal, methodDefinition)) 
                        return;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"An error occurred with 'GetBranchPointsForToken' for method '{methodDefinition.FullName}'", ex);
            }
        }

        private bool BuildPointsForConditionalBranch(List<BranchPoint> list, Instruction instruction,
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

        private uint BuildPointsForBranch(List<BranchPoint> list, Instruction then, int branchingInstructionLine, string document,
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

        private uint BuildPointsForSwitchCases(List<BranchPoint> list, BranchPoint path0, Instruction[] branchInstructions,
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
                .Where( e =>branchInstruction.Offset < e.HandlerEnd.Maybe(h => h.Offset, GetOffsetOfNextEndfinally(methodDefinition.Body, e.HandlerStart.Offset)))
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

        private List<int> GetBranchPath(Instruction instruction)
        {
            var offsetList = new List<int>();

            if (instruction != null)
            {
                var point = instruction;
                offsetList.Add(point.Offset);
                while ( point.OpCode == OpCodes.Br || point.OpCode == OpCodes.Br_S )
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