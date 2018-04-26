using System.Collections.Generic;
using System.IO;
using System.Linq;

using Coverlet.Core.Helpers;
using Coverlet.Core.Extensions;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Xml.Linq;

namespace Coverlet.Core.Instrumentation
{
    internal class Instrumenter
    {
        private string _module;
        private string _identifier;
        private IEnumerable<string> _excludedFiles;
        private InstrumenterResult _result;

        public Instrumenter(string module, string identifier, IEnumerable<string> excludedFiles = null)
        {
            _module = module;
            _identifier = identifier;
            _excludedFiles = excludedFiles;
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
            {
                var resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(Path.GetDirectoryName(_module));
                var parameters = new ReaderParameters { ReadSymbols = true, AssemblyResolver = resolver };
                ModuleDefinition module = ModuleDefinition.ReadModule(stream, parameters);

                foreach (var type in module.GetTypes())
                {
                    if (type.CustomAttributes.Any(a => a.AttributeType.Name == "ExcludeFromCoverageAttribute" || a.AttributeType.Name == "ExcludeFromCoverage"))
                        continue;

                    foreach (var method in type.Methods)
                    {
                        var sourceFiles = method.DebugInformation.SequencePoints.Select(s => s.Document.Url).Distinct();
                        if (_excludedFiles != null && sourceFiles.Any(_excludedFiles.Contains)) {
                            continue;
                        }
                        if (!method.CustomAttributes.Any(a => a.AttributeType.Name == "ExcludeFromCoverageAttribute" || a.AttributeType.Name == "ExcludeFromCoverage"))
                            InstrumentMethod(method);
                    }
                }

                module.Write(stream);
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
            ILProcessor processor = method.Body.GetILProcessor();

            var index = 0;
            var count = processor.Body.Instructions.Count;

            for (int n = 0; n < count; n++)
            {
                var instruction = processor.Body.Instructions[index];
                var sequencePoint = method.DebugInformation.GetSequencePoint(instruction);
                if (sequencePoint == null || sequencePoint.StartLine == 16707566)
                {
                    index++;
                    continue;
                }

                var target = AddInstrumentationCode(method, processor, instruction, sequencePoint);
                foreach (var _instruction in processor.Body.Instructions)
                    ReplaceInstructionTarget(_instruction, instruction, target);

                foreach (ExceptionHandler handler in processor.Body.ExceptionHandlers)
                    ReplaceExceptionHandlerBoundary(handler, instruction, target);

                index += 4;
            }

            method.Body.SimplifyMacros();
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

            string flag = IsBranchTarget(processor, instruction) ? "B" : "L";
            string marker = $"{document.Path},{sequencePoint.StartLine},{sequencePoint.EndLine},{flag}";

            var pathInstr = Instruction.Create(OpCodes.Ldstr, _result.HitsFilePath);
            var markInstr = Instruction.Create(OpCodes.Ldstr, marker);
            var callInstr = Instruction.Create(OpCodes.Call, processor.Body.Method.Module.ImportReference(typeof(CoverageTracker).GetMethod("MarkExecuted")));

            processor.InsertBefore(instruction, callInstr);
            processor.InsertBefore(callInstr, markInstr);
            processor.InsertBefore(markInstr, pathInstr);

            return pathInstr;
        }

        private bool IsBranchTarget(ILProcessor processor, Instruction instruction)
        {
            foreach (var _instruction in processor.Body.Instructions)
            {
                if (_instruction.Operand is Instruction target)
                {
                    if (target == instruction)
                        return true;
                }

                if (_instruction.Operand is Instruction[] targets)
                    return targets.Any(t => t == instruction);
            }

            return false;
        }

        private void ReplaceInstructionTarget(Instruction instruction, Instruction oldTarget, Instruction newTarget)
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

        private void ReplaceExceptionHandlerBoundary(ExceptionHandler handler, Instruction oldTarget, Instruction newTarget)
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
    }
}