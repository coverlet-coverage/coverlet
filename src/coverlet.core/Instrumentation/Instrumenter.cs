using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Coverlet.Core.Attributes;
using Coverlet.Core.Helpers;
using Coverlet.Core.Symbols;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Coverlet.Core.Instrumentation
{
    internal class Instrumenter
    {
        private readonly string _module;
        private readonly string _identifier;
        private readonly IEnumerable<string> _excludedFiles;
        private readonly static Lazy<MethodInfo> _markExecutedMethodLoader = new Lazy<MethodInfo>(GetMarkExecutedMethod);
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
                        InstrumentType(type);
                    }

                    module.Write(stream);
                }
            }
        }

        private void InstrumentType(TypeDefinition type)
        {
            if (type.CustomAttributes.Any(IsExcludeAttribute))
                return;

            foreach (var method in type.Methods)
            {
                if (!method.CustomAttributes.Any(IsExcludeAttribute))
                    InstrumentMethod(method);
            }
        }

        private void InstrumentMethod(MethodDefinition method)
        {
            var sourceFile = method.DebugInformation.SequencePoints.Select(s => s.Document.Url).FirstOrDefault();
            if (!string.IsNullOrEmpty(sourceFile) && _excludedFiles.Contains(sourceFile))
                return;

            var methodBody = GetMethodBody(method);
            if (methodBody == null)
                return;

            if (method.IsNative)
                return;

            InstrumentIL(method);
        }

        private void InstrumentIL(MethodDefinition method)
        {
            method.Body.SimplifyMacros();
            ILProcessor processor = method.Body.GetILProcessor();

            var index = 0;
            var count = processor.Body.Instructions.Count;

            var branchPoints = CecilSymbolHelper.GetBranchPoints(method);

            for (int n = 0; n < count; n++)
            {
                var instruction = processor.Body.Instructions[index];
                var sequencePoint = method.DebugInformation.GetSequencePoint(instruction);
                var targetedBranchPoints = branchPoints.Where(p => p.EndOffset == instruction.Offset);
                
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
                        /*
                         * Skip branches with no sequence point reference for now.
                         * In this case for an anonymous class the compiler will dynamically create an Equals 'utility' method.
                         * The CecilSymbolHelper will create branch points with a start line of -1 and no document, which
                         * I am currently not sure how to handle.
                         */
                        if (_branchTarget.StartLine == -1 || _branchTarget.Document == null)
                            continue;
                        
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
            string marker = $"L,{document.Path},{sequencePoint.StartLine},{sequencePoint.EndLine}";

            var pathInstr = Instruction.Create(OpCodes.Ldstr, _result.HitsFilePath);
            var markInstr = Instruction.Create(OpCodes.Ldstr, marker);
            var callInstr = Instruction.Create(OpCodes.Call, processor.Body.Method.Module.ImportReference(_markExecutedMethodLoader.Value));

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

            if (!document.Branches.Exists(l => l.Number == branchPoint.StartLine && l.Ordinal == branchPoint.Ordinal))
                document.Branches.Add(
                    new Branch
                    {
                        Number = branchPoint.StartLine,
                        Class = method.DeclaringType.FullName,
                        Method = method.FullName,
                        Offset = branchPoint.Offset,
                        EndOffset = branchPoint.EndOffset,
                        Path = branchPoint.Path,
                        Ordinal = branchPoint.Ordinal
                    }
                );

            string marker = $"B,{document.Path},{branchPoint.StartLine},{branchPoint.Ordinal}";

            var pathInstr = Instruction.Create(OpCodes.Ldstr, _result.HitsFilePath);
            var markInstr = Instruction.Create(OpCodes.Ldstr, marker);
            var callInstr = Instruction.Create(OpCodes.Call, processor.Body.Method.Module.ImportReference(_markExecutedMethodLoader.Value));

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

        private static bool IsExcludeAttribute(CustomAttribute customAttribute)
        {
            return customAttribute.AttributeType.Name == nameof(ExcludeFromCoverageAttribute) || customAttribute.AttributeType.Name == "ExcludeFromCoverage";
        }

        private static Mono.Cecil.Cil.MethodBody GetMethodBody(MethodDefinition method)
        {
            try
            {
                return method.HasBody ? method.Body : null;
            }
            catch
            {
                return null;
            }
        }

        private static MethodInfo GetMarkExecutedMethod()
        {
            return typeof(CoverageTracker).GetMethod(nameof(CoverageTracker.MarkExecuted));
        }
    }
}