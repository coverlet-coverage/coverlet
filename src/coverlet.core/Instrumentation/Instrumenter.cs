using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Coverlet.Core.Helpers;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Coverlet.Core.Instrumentation
{
    internal class Instrumenter
    {
        private string _module;
        private string _identifier;
        private InstrumenterResult _result;

        public Instrumenter(string module, string identifier)
        {
            _module = module;
            _identifier = identifier;
        }

        public InstrumenterResult Instrument()
        {
            string reportPath = Path.Combine(
                Path.GetTempPath(),
                Path.GetFileNameWithoutExtension(_module) + "_" + _identifier
            );

            string originalModuleTempPath = Path.Combine(
                Path.GetTempPath(),
                Path.GetFileNameWithoutExtension(_module) + "_" + _identifier + ".dll"
            );

            File.Copy(_module, originalModuleTempPath);

            _result = new InstrumenterResult
            {
                Module = Path.GetFileNameWithoutExtension(_module),
                ReportPath = reportPath,
                OriginalModulePath = _module,
                OriginalModuleTempPath = originalModuleTempPath
            };

            InstrumentModule();
            InstrumentationHelper.CopyCoverletDependency(Path.GetDirectoryName(_module));

            return _result;
        }

        private void InstrumentModule()
        {
            using (var stream = new FileStream(_module, FileMode.Open, FileAccess.ReadWrite))
            {
                var parameters = new ReaderParameters { ReadSymbols = true };
                ModuleDefinition module = ModuleDefinition.ReadModule(stream, parameters);

                foreach (var type in module.GetTypes())
                {
                    foreach (var method in type.Methods)
                        InstrumentMethod(method);
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
            var instructions = processor.Body.Instructions.ToList();

            var ifTargets = instructions
                .Where(i => (i.Operand as Instruction) != null)
                .Select(i => (i.Operand as Instruction).Offset);

            var targetInstructions = new Dictionary<int, Instruction>();
            foreach (var instruction in instructions)
            {
                if (ifTargets.Contains(instruction.Offset))
                    targetInstructions.Add(instruction.Offset, Instruction.Create(OpCodes.Nop));
            }

            processor.Body.Instructions.Clear();

            foreach (var instruction in instructions)
            {
                Instruction _instruction = default(Instruction);
                switch (instruction.OpCode.OperandType)
                {
                    case OperandType.InlineNone:
                        _instruction = Instruction.Create(instruction.OpCode);
                        break;
                    case OperandType.InlineI:
                        _instruction = Instruction.Create(instruction.OpCode, (int)instruction.Operand);
                        break;
                    case OperandType.InlineI8:
                        _instruction = Instruction.Create(instruction.OpCode, (long)instruction.Operand);
                        break;
                    case OperandType.ShortInlineI:
                        if (instruction.OpCode == OpCodes.Ldc_I4_S)
                            _instruction = Instruction.Create(instruction.OpCode, (sbyte)instruction.Operand);
                        else
                            _instruction = Instruction.Create(instruction.OpCode, (byte)instruction.Operand);
                        break;
                    case OperandType.InlineR:
                        _instruction = Instruction.Create(instruction.OpCode, (double)instruction.Operand);
                        break;
                    case OperandType.ShortInlineR:
                        _instruction = Instruction.Create(instruction.OpCode, (float)instruction.Operand);
                        break;
                    case OperandType.InlineString:
                        _instruction = Instruction.Create(instruction.OpCode, (string)instruction.Operand);
                        break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.InlineBrTarget:
                        OpCode opCode = default(OpCode);
                        // Offset values could change and not be short form anymore
                        if (instruction.OpCode == OpCodes.Br_S)
                            opCode = OpCodes.Br;
                        else if (instruction.OpCode == OpCodes.Brfalse_S)
                            opCode = OpCodes.Brfalse;
                        else if (instruction.OpCode == OpCodes.Brtrue_S)
                            opCode = OpCodes.Brtrue;
                        else if (instruction.OpCode == OpCodes.Beq_S)
                            opCode = OpCodes.Beq;
                        else if (instruction.OpCode == OpCodes.Bge_S)
                            opCode = OpCodes.Bge;
                        else if (instruction.OpCode == OpCodes.Bgt_S)
                            opCode = OpCodes.Bgt;
                        else if (instruction.OpCode == OpCodes.Ble_S)
                            opCode = OpCodes.Ble;
                        else if (instruction.OpCode == OpCodes.Blt_S)
                            opCode = OpCodes.Blt;
                        else if (instruction.OpCode == OpCodes.Bne_Un_S)
                            opCode = OpCodes.Bne_Un;
                        else if (instruction.OpCode == OpCodes.Bge_Un_S)
                            opCode = OpCodes.Bge_Un;
                        else if (instruction.OpCode == OpCodes.Bgt_Un_S)
                            opCode = OpCodes.Bgt;
                        else if (instruction.OpCode == OpCodes.Ble_Un_S)
                            opCode = OpCodes.Ble;
                        else if (instruction.OpCode == OpCodes.Blt_Un_S)
                            opCode = OpCodes.Blt;
                        else if (instruction.OpCode == OpCodes.Leave_S)
                            opCode = OpCodes.Leave;
                        else
                            opCode = instruction.OpCode;

                        var target = (Instruction)instruction.Operand;
                        _instruction = Instruction.Create(opCode, targetInstructions[target.Offset]);
                        break;
                    case OperandType.ShortInlineVar:
                    case OperandType.InlineVar:
                        var variable = (VariableDefinition)instruction.Operand;
                        _instruction = Instruction.Create(instruction.OpCode, variable);
                        break;
                    case OperandType.ShortInlineArg:
                    case OperandType.InlineArg:
                        var parameter = (ParameterDefinition)instruction.Operand;
                        _instruction = Instruction.Create(instruction.OpCode, parameter);
                        break;
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                        _instruction = Instruction.Create(instruction.OpCode, (TypeReference)instruction.Operand);
                        break;
                    case OperandType.InlineField:
                        _instruction = Instruction.Create(instruction.OpCode, (FieldReference)instruction.Operand);
                        break;
                    case OperandType.InlineMethod:
                        _instruction = Instruction.Create(instruction.OpCode, (MethodReference)instruction.Operand);
                        break;
                    default:
                        throw new NotSupportedException(instruction.ToString());
                }

                AddInstrumentationCode(
                    processor,
                    method.DebugInformation.GetSequencePoint(instruction));

                if (ifTargets.Contains(instruction.Offset))
                {
                    targetInstructions[instruction.Offset].Offset = _instruction.Offset;
                    targetInstructions[instruction.Offset].OpCode = _instruction.OpCode;
                    targetInstructions[instruction.Offset].Operand = _instruction.Operand;
                    targetInstructions[instruction.Offset].Previous = _instruction.Previous;
                    targetInstructions[instruction.Offset].Next = _instruction.Next;
                    processor.Append(targetInstructions[instruction.Offset]);
                }
                else
                    processor.Append(_instruction);
            }
        }

        private void AddInstrumentationCode(ILProcessor processor, SequencePoint sequencePoint)
        {
            if (sequencePoint == null || sequencePoint.StartLine == 16707566)
                return;

            var document = _result.Documents.FirstOrDefault(d => d.Path == sequencePoint.Document.Url);
            if (document == null)
            {
                document = new Document { Path = sequencePoint.Document.Url };
                _result.Documents.Add(document);
            }

            for (int i = sequencePoint.StartLine; i <= sequencePoint.EndLine; i++)
            {
                if (!document.Lines.Exists(l => l.Number == i))
                    document.Lines.Add(new Line { Number = i });
            }

            string marker = $"{document.Path}:{sequencePoint.StartLine}:{sequencePoint.EndLine}";
            processor.Append(Instruction.Create(OpCodes.Ldstr, _result.ReportPath));
            processor.Append(Instruction.Create(OpCodes.Ldstr, marker));
            processor.Append(Instruction.Create(OpCodes.Call, processor.Body.Method.Module.ImportReference(typeof(CoverageTracker).GetMethod("MarkExecuted"))));
        }
    }
}