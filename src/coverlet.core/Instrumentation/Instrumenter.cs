// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Attributes;
using Coverlet.Core.Helpers;
using Coverlet.Core.Instrumentation.Reachability;
using Coverlet.Core.Symbols;
using Microsoft.Extensions.FileSystemGlobbing;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Coverlet.Core.Instrumentation
{
    internal class Instrumenter
    {
        private readonly string _module;
        private readonly string _identifier;
        private readonly ExcludedFilesHelper _excludedFilesHelper;
        private readonly CoverageParameters _parameters;
        private readonly string[] _excludedAttributes;
        private readonly bool _isCoreLibrary;
        private readonly ILogger _logger;
        private readonly IInstrumentationHelper _instrumentationHelper;
        private readonly IFileSystem _fileSystem;
        private readonly ISourceRootTranslator _sourceRootTranslator;
        private readonly ICecilSymbolHelper _cecilSymbolHelper;
        private InstrumenterResult _result;
        private FieldDefinition _customTrackerHitsArray;
        private FieldDefinition _customTrackerHitsFilePath;
        private FieldDefinition _customTrackerSingleHit;
        private FieldDefinition _customTrackerFlushHitFile;
        private ILProcessor _customTrackerClassConstructorIl;
        private TypeDefinition _customTrackerTypeDef;
        private MethodReference _customTrackerRegisterUnloadEventsMethod;
        private MethodReference _customTrackerRecordHitMethod;
        private List<string> _excludedSourceFiles;
        private List<string> _branchesInCompiledGeneratedClass;
        private List<(MethodDefinition, int)> _excludedMethods;
        private List<string> _excludedLambdaMethods;
        private List<string> _excludedCompilerGeneratedTypes;
        private readonly string[] _doesNotReturnAttributes;
        private ReachabilityHelper _reachabilityHelper;

        public bool SkipModule { get; set; }

        public Instrumenter(
            string module,
            string identifier,
            CoverageParameters parameters,
            ILogger logger,
            IInstrumentationHelper instrumentationHelper,
            IFileSystem fileSystem,
            ISourceRootTranslator sourceRootTranslator,
            ICecilSymbolHelper cecilSymbolHelper)
        {
            _module = module;
            _identifier = identifier;
            _parameters = parameters;
            _excludedFilesHelper = new ExcludedFilesHelper(parameters.ExcludedSourceFiles, logger);
            _excludedAttributes = PrepareAttributes(parameters.ExcludeAttributes, nameof(ExcludeFromCoverageAttribute), nameof(ExcludeFromCodeCoverageAttribute));
            _isCoreLibrary = Path.GetFileNameWithoutExtension(_module) == "System.Private.CoreLib";
            _logger = logger;
            _instrumentationHelper = instrumentationHelper;
            _fileSystem = fileSystem;
            _sourceRootTranslator = sourceRootTranslator;
            _cecilSymbolHelper = cecilSymbolHelper;
            _doesNotReturnAttributes = PrepareAttributes(parameters.DoesNotReturnAttributes);
        }

        private static string[] PrepareAttributes(IEnumerable<string> providedAttrs, params string[] defaultAttrs)
        {
            return
                (providedAttrs ?? Array.Empty<string>())
                // In case the attribute class ends in "Attribute", but it wasn't specified.
                // Both names are included (if it wasn't specified) because the attribute class might not actually end in the prefix.
                .SelectMany(a => a.EndsWith("Attribute") ? new[] { a } : new[] { a, $"{a}Attribute" })
                // The default custom attributes used to exclude from coverage.
                .Union(defaultAttrs)
                .ToArray();
        }

        public bool CanInstrument()
        {
            try
            {
                if (_instrumentationHelper.HasPdb(_module, out bool embeddedPdb))
                {
                    if (this._parameters.InstrumentModulesWithoutLocalSources)
                    {
                        return true;
                    }
                    if (embeddedPdb)
                    {
                        if (_instrumentationHelper.EmbeddedPortablePdbHasLocalSource(_module, out string firstNotFoundDocument))
                        {
                            return true;
                        }
                        else
                        {
                            _logger.LogVerbose($"Unable to instrument module: {_module}, embedded pdb without local source files, [{FileSystem.EscapeFileName(firstNotFoundDocument)}]");
                            return false;
                        }
                    }
                    else
                    {
                        if (_instrumentationHelper.PortablePdbHasLocalSource(_module, out string firstNotFoundDocument))
                        {
                            return true;
                        }
                        else
                        {
                            _logger.LogVerbose($"Unable to instrument module: {_module}, pdb without local source files, [{FileSystem.EscapeFileName(firstNotFoundDocument)}]");
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Unable to instrument module: '{_module}'\n{ex}");
                return false;
            }
        }

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

            if (_excludedSourceFiles != null)
            {
                foreach (string sourceFile in _excludedSourceFiles)
                {
                    _logger.LogVerbose($"Excluded source file: '{FileSystem.EscapeFileName(sourceFile)}'");
                }
            }

            _result.BranchesInCompiledGeneratedClass = _branchesInCompiledGeneratedClass == null ? Array.Empty<string>() : _branchesInCompiledGeneratedClass.ToArray();

            return _result;
        }

        // If current type or one of his parent is excluded we'll exclude it
        // If I'm out every my children and every children of my children will be out
        private bool IsTypeExcluded(TypeDefinition type)
        {
            for (TypeDefinition current = type; current != null; current = current.DeclaringType)
            {
                // Check exclude attribute and filters
                if (current.CustomAttributes.Any(IsExcludeAttribute) || _instrumentationHelper.IsTypeExcluded(_module, current.FullName, _parameters.ExcludeFilters))
                {
                    return true;
                }
            }

            return false;
        }

        // Instrumenting Interlocked which is used for recording hits would cause an infinite loop.
        private bool Is_System_Threading_Interlocked_CoreLib_Type(TypeDefinition type)
        {
            return _isCoreLibrary && type.FullName == "System.Threading.Interlocked";
        }

        // Have to do this before we start writing to a module, as we'll get into file
        // locking issues if we do it while writing.
        private void CreateReachabilityHelper()
        {
            using Stream stream = _fileSystem.NewFileStream(_module, FileMode.Open, FileAccess.Read);
            using var resolver = new NetstandardAwareAssemblyResolver(_module, _logger);
            resolver.AddSearchDirectory(Path.GetDirectoryName(_module));
            var parameters = new ReaderParameters { ReadSymbols = true, AssemblyResolver = resolver };
            if (_isCoreLibrary)
            {
                parameters.MetadataImporterProvider = new CoreLibMetadataImporterProvider();
            }

            using var module = ModuleDefinition.ReadModule(stream, parameters);
            _reachabilityHelper = ReachabilityHelper.CreateForModule(module, _doesNotReturnAttributes, _logger);
        }

        private void InstrumentModule()
        {
            CreateReachabilityHelper();

            using Stream stream = _fileSystem.NewFileStream(_module, FileMode.Open, FileAccess.ReadWrite);
            using var resolver = new NetstandardAwareAssemblyResolver(_module, _logger);
            resolver.AddSearchDirectory(Path.GetDirectoryName(_module));
            var parameters = new ReaderParameters { ReadSymbols = true, AssemblyResolver = resolver };
            if (_isCoreLibrary)
            {
                parameters.MetadataImporterProvider = new CoreLibMetadataImporterProvider();
            }

            using var module = ModuleDefinition.ReadModule(stream, parameters);
            foreach (CustomAttribute customAttribute in module.Assembly.CustomAttributes)
            {
                if (IsExcludeAttribute(customAttribute))
                {
                    _logger.LogVerbose($"Excluded module: '{module}' for assembly level attribute {customAttribute.AttributeType.FullName}");
                    SkipModule = true;
                    return;
                }
            }

            bool containsAppContext = module.GetType(nameof(System), nameof(AppContext)) != null;
            IEnumerable<TypeDefinition> types = module.GetTypes();
            AddCustomModuleTrackerToModule(module);

            CustomDebugInformation sourceLinkDebugInfo = module.CustomDebugInformations.FirstOrDefault(c => c.Kind == CustomDebugInformationKind.SourceLink);
            if (sourceLinkDebugInfo != null)
            {
                _result.SourceLink = ((SourceLinkDebugInformation)sourceLinkDebugInfo).Content;
            }

            foreach (TypeDefinition type in types)
            {
                if (
                    !Is_System_Threading_Interlocked_CoreLib_Type(type) &&
                    !IsTypeExcluded(type) &&
                    _instrumentationHelper.IsTypeIncluded(_module, type.FullName, _parameters.IncludeFilters)
                    )
                {
                    if (IsSynthesizedMemberToBeExcluded(type))
                    {
                        (_excludedCompilerGeneratedTypes ??= new List<string>()).Add(type.FullName);
                    }
                    else
                    {
                        InstrumentType(type);
                    }
                }
            }

            // Fixup the custom tracker class constructor, according to all instrumented types
            if (_customTrackerRegisterUnloadEventsMethod == null)
            {
                _customTrackerRegisterUnloadEventsMethod = new MethodReference(
                    nameof(ModuleTrackerTemplate.RegisterUnloadEvents), module.TypeSystem.Void, _customTrackerTypeDef);
            }

            Instruction lastInstr = _customTrackerClassConstructorIl.Body.Instructions.Last();

            if (!containsAppContext)
            {
                // For "normal" cases, where the instrumented assembly is not the core library, we add a call to
                // RegisterUnloadEvents to the static constructor of the generated custom tracker. Due to static
                // initialization constraints, the core library is handled separately below.
                _customTrackerClassConstructorIl.InsertBefore(lastInstr, Instruction.Create(OpCodes.Call, _customTrackerRegisterUnloadEventsMethod));
            }

            _customTrackerClassConstructorIl.InsertBefore(lastInstr, Instruction.Create(OpCodes.Ldc_I4, _result.HitCandidates.Count));
            _customTrackerClassConstructorIl.InsertBefore(lastInstr, Instruction.Create(OpCodes.Newarr, module.TypeSystem.Int32));
            _customTrackerClassConstructorIl.InsertBefore(lastInstr, Instruction.Create(OpCodes.Stsfld, _customTrackerHitsArray));
            _customTrackerClassConstructorIl.InsertBefore(lastInstr, Instruction.Create(OpCodes.Ldstr, _result.HitsFilePath));
            _customTrackerClassConstructorIl.InsertBefore(lastInstr, Instruction.Create(OpCodes.Stsfld, _customTrackerHitsFilePath));
            _customTrackerClassConstructorIl.InsertBefore(lastInstr, Instruction.Create(_parameters.SingleHit ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
            _customTrackerClassConstructorIl.InsertBefore(lastInstr, Instruction.Create(OpCodes.Stsfld, _customTrackerSingleHit));
            _customTrackerClassConstructorIl.InsertBefore(lastInstr, Instruction.Create(OpCodes.Ldc_I4_1));
            _customTrackerClassConstructorIl.InsertBefore(lastInstr, Instruction.Create(OpCodes.Stsfld, _customTrackerFlushHitFile));

            if (containsAppContext)
            {
                // Handle the core library by instrumenting System.AppContext.OnProcessExit to directly call
                // the UnloadModule method of the custom tracker type. This avoids loops between the static
                // initialization of the custom tracker and the static initialization of the hosting AppDomain
                // (which for the core library case will be instrumented code).
                var eventArgsType = new TypeReference(nameof(System), nameof(EventArgs), module, module.TypeSystem.CoreLibrary);
                var customTrackerUnloadModule = new MethodReference(nameof(ModuleTrackerTemplate.UnloadModule), module.TypeSystem.Void, _customTrackerTypeDef);
                customTrackerUnloadModule.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
                customTrackerUnloadModule.Parameters.Add(new ParameterDefinition(eventArgsType));

                var appContextType = new TypeReference(nameof(System), nameof(AppContext), module, module.TypeSystem.CoreLibrary);
                MethodDefinition onProcessExitMethod = new MethodReference("OnProcessExit", module.TypeSystem.Void, appContextType).Resolve();
                ILProcessor onProcessExitIl = onProcessExitMethod.Body.GetILProcessor();

                // Put the OnProcessExit body inside try/finally to ensure the call to the UnloadModule.
                Instruction lastInst = onProcessExitMethod.Body.Instructions.Last();
                var firstNullParam = Instruction.Create(OpCodes.Ldnull);
                var secondNullParam = Instruction.Create(OpCodes.Ldnull);
                var callUnload = Instruction.Create(OpCodes.Call, customTrackerUnloadModule);
                onProcessExitIl.InsertAfter(lastInst, firstNullParam);
                onProcessExitIl.InsertAfter(firstNullParam, secondNullParam);
                onProcessExitIl.InsertAfter(secondNullParam, callUnload);
                var endFinally = Instruction.Create(OpCodes.Endfinally);
                onProcessExitIl.InsertAfter(callUnload, endFinally);
                Instruction ret = onProcessExitIl.Create(OpCodes.Ret);
                Instruction leaveAfterFinally = onProcessExitIl.Create(OpCodes.Leave, ret);
                onProcessExitIl.InsertAfter(endFinally, ret);
                foreach (Instruction inst in onProcessExitMethod.Body.Instructions.ToArray())
                {
                    // Patch ret to leave after the finally
                    if (inst.OpCode == OpCodes.Ret && inst != ret)
                    {
                        Instruction leaveBodyInstAfterFinally = onProcessExitIl.Create(OpCodes.Leave, ret);
                        Instruction prevInst = inst.Previous;
                        onProcessExitMethod.Body.Instructions.Remove(inst);
                        onProcessExitIl.InsertAfter(prevInst, leaveBodyInstAfterFinally);
                    }
                }
                var handler = new ExceptionHandler(ExceptionHandlerType.Finally)
                {
                    TryStart = onProcessExitIl.Body.Instructions.First(),
                    TryEnd = firstNullParam,
                    HandlerStart = firstNullParam,
                    HandlerEnd = ret
                };

                onProcessExitMethod.Body.ExceptionHandlers.Add(handler);
            }

            module.Write(stream, new WriterParameters { WriteSymbols = true });
        }

        private void AddCustomModuleTrackerToModule(ModuleDefinition module)
        {
            using (var coverletInstrumentationAssembly = AssemblyDefinition.ReadAssembly(typeof(ModuleTrackerTemplate).Assembly.Location))
            {
                TypeDefinition moduleTrackerTemplate = coverletInstrumentationAssembly.MainModule.GetType(
                    "Coverlet.Core.Instrumentation", nameof(ModuleTrackerTemplate));

                _customTrackerTypeDef = new TypeDefinition(
                    "Coverlet.Core.Instrumentation.Tracker", Path.GetFileNameWithoutExtension(module.Name) + "_" + _identifier, moduleTrackerTemplate.Attributes);

                _customTrackerTypeDef.BaseType = module.TypeSystem.Object;
                foreach (FieldDefinition fieldDef in moduleTrackerTemplate.Fields)
                {
                    var fieldClone = new FieldDefinition(fieldDef.Name, fieldDef.Attributes, fieldDef.FieldType);
                    fieldClone.FieldType = module.ImportReference(fieldDef.FieldType);

                    _customTrackerTypeDef.Fields.Add(fieldClone);

                    if (fieldClone.Name == nameof(ModuleTrackerTemplate.HitsArray))
                        _customTrackerHitsArray = fieldClone;
                    else if (fieldClone.Name == nameof(ModuleTrackerTemplate.HitsFilePath))
                        _customTrackerHitsFilePath = fieldClone;
                    else if (fieldClone.Name == nameof(ModuleTrackerTemplate.SingleHit))
                        _customTrackerSingleHit = fieldClone;
                    else if (fieldClone.Name == nameof(ModuleTrackerTemplate.FlushHitFile))
                        _customTrackerFlushHitFile = fieldClone;
                }

                foreach (MethodDefinition methodDef in moduleTrackerTemplate.Methods)
                {
                    var methodOnCustomType = new MethodDefinition(methodDef.Name, methodDef.Attributes, methodDef.ReturnType);

                    foreach (ParameterDefinition parameter in methodDef.Parameters)
                    {
                        methodOnCustomType.Parameters.Add(new ParameterDefinition(module.ImportReference(parameter.ParameterType)));
                    }

                    foreach (VariableDefinition variable in methodDef.Body.Variables)
                    {
                        methodOnCustomType.Body.Variables.Add(new VariableDefinition(module.ImportReference(variable.VariableType)));
                    }

                    methodOnCustomType.Body.InitLocals = methodDef.Body.InitLocals;

                    ILProcessor ilProcessor = methodOnCustomType.Body.GetILProcessor();
                    if (methodDef.Name == ".cctor")
                        _customTrackerClassConstructorIl = ilProcessor;

                    foreach (Instruction instr in methodDef.Body.Instructions)
                    {
                        if (instr.Operand is MethodReference methodReference)
                        {
                            if (!methodReference.FullName.Contains(moduleTrackerTemplate.Namespace))
                            {
                                // External method references, just import then
                                instr.Operand = module.ImportReference(methodReference);
                            }
                            else
                            {
                                // Move to the custom type
                                var updatedMethodReference = new MethodReference(methodReference.Name, methodReference.ReturnType, _customTrackerTypeDef);
                                foreach (ParameterDefinition parameter in methodReference.Parameters)
                                    updatedMethodReference.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, module.ImportReference(parameter.ParameterType)));

                                instr.Operand = updatedMethodReference;
                            }
                        }
                        else if (instr.Operand is FieldReference fieldReference)
                        {
                            instr.Operand = _customTrackerTypeDef.Fields.Single(fd => fd.Name == fieldReference.Name);
                        }
                        else if (instr.Operand is TypeReference typeReference)
                        {
                            instr.Operand = module.ImportReference(typeReference);
                        }

                        ilProcessor.Append(instr);
                    }

                    foreach (ExceptionHandler handler in methodDef.Body.ExceptionHandlers)
                    {
                        if (handler.CatchType != null)
                        {
                            handler.CatchType = module.ImportReference(handler.CatchType);
                        }

                        methodOnCustomType.Body.ExceptionHandlers.Add(handler);
                    }

                    _customTrackerTypeDef.Methods.Add(methodOnCustomType);
                }

                module.Types.Add(_customTrackerTypeDef);
            }

            Debug.Assert(_customTrackerHitsArray != null);
            Debug.Assert(_customTrackerClassConstructorIl != null);
        }

        private bool IsMethodOfCompilerGeneratedClassOfAsyncStateMachineToBeExcluded(MethodDefinition method)
        {
            // Type compiler generated, the async state machine
            TypeDefinition typeDefinition = method.DeclaringType;
            if (typeDefinition.DeclaringType is null)
            {
                return false;
            }

            // Search in type that contains async state machine, compiler generates async state machine in private nested class
            foreach (MethodDefinition typeMethod in typeDefinition.DeclaringType.Methods)
            {
                // If we find the async state machine attribute on method
                CustomAttribute attribute;
                if ((attribute = typeMethod.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == typeof(AsyncStateMachineAttribute).FullName)) != null)
                {
                    // If the async state machine generated by compiler is "associated" to this method we check for exclusions
                    // The associated type is specified on attribute constructor 
                    // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.asyncstatemachineattribute.-ctor?view=netcore-3.1
                    if (attribute.ConstructorArguments[0].Value == method.DeclaringType)
                    {
                        if (typeMethod.CustomAttributes.Any(IsExcludeAttribute))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void InstrumentType(TypeDefinition type)
        {
            IEnumerable<MethodDefinition> methods = type.GetMethods();

            // We keep ordinal index because it's the way used by compiler for generated types/methods to 
            // avoid ambiguity
            int ordinal = -1;
            foreach (MethodDefinition method in methods)
            {
                MethodDefinition actualMethod = method;
                IEnumerable<CustomAttribute> customAttributes = method.CustomAttributes;
                if (_instrumentationHelper.IsLocalMethod(method.Name))
                    actualMethod = methods.FirstOrDefault(m => m.Name == method.Name.Split('>')[0].Substring(1)) ?? method;

                if (actualMethod.IsGetter || actualMethod.IsSetter)
                {
                    if (_parameters.SkipAutoProps && actualMethod.CustomAttributes.Any(ca => ca.AttributeType.FullName == typeof(CompilerGeneratedAttribute).FullName))
                    {
                        continue;
                    }

                    PropertyDefinition prop = type.Properties.FirstOrDefault(p => p.GetMethod?.FullName.Equals(actualMethod.FullName) == true ||
                                                                                    p.SetMethod?.FullName.Equals(actualMethod.FullName) == true);
                    if (prop?.HasCustomAttributes == true)
                        customAttributes = customAttributes.Union(prop.CustomAttributes);
                }

                ordinal++;

                if (IsMethodOfCompilerGeneratedClassOfAsyncStateMachineToBeExcluded(method))
                {
                    continue;
                }

                if (IsSynthesizedMemberToBeExcluded(method))
                {
                    continue;
                }

                if (_excludedLambdaMethods != null && _excludedLambdaMethods.Contains(method.FullName))
                {
                    continue;
                }

                if (!customAttributes.Any(IsExcludeAttribute))
                {
                    InstrumentMethod(method);
                }
                else
                {
                    (_excludedLambdaMethods ??= new List<string>()).AddRange(CollectLambdaMethodsInsideLocalFunction(method));
                    (_excludedMethods ??= new List<(MethodDefinition, int)>()).Add((method, ordinal));
                }
            }

            IEnumerable<MethodDefinition> ctors = type.GetConstructors();
            foreach (MethodDefinition ctor in ctors)
            {
                if (!ctor.CustomAttributes.Any(IsExcludeAttribute))
                {
                    InstrumentMethod(ctor);
                }
            }
        }

        private void InstrumentMethod(MethodDefinition method)
        {
            string sourceFile = method.DebugInformation.SequencePoints.Select(s => _sourceRootTranslator.ResolveFilePath(s.Document.Url)).FirstOrDefault();
            if (!string.IsNullOrEmpty(sourceFile) && _excludedFilesHelper.Exclude(sourceFile))
            {
                if (!(_excludedSourceFiles ??= new List<string>()).Contains(sourceFile))
                {
                    _excludedSourceFiles.Add(sourceFile);
                }
                return;
            }

            MethodBody methodBody = GetMethodBody(method);
            if (methodBody == null)
                return;

            if (method.IsNative)
                return;

            InstrumentIL(method);
        }

        /// <summary>
        /// The base idea is to inject an int placeholder for every sequence point. We register source+placeholder+lines(from sequence point) for final accounting.
        /// Instrumentation alg(current instruction: instruction we're analyzing):
        /// 1) We get all branches for the method
        /// 2) We get the sequence point of every instruction of method(start line/end line)
        /// 3) We check if current instruction is reachable and coverable
        /// 4) For every sequence point of an instruction we put load(int hint placeholder)+call opcode above current instruction
        /// 5) We patch all jump to current instruction with first injected instruction(load)
        /// 6) If current instruction is a target for a branch we inject again load(int hint placeholder)+call opcode above current instruction
        /// 7) We patch all jump to current instruction with first injected instruction(load)
        /// </summary>
        private void InstrumentIL(MethodDefinition method)
        {
            method.Body.SimplifyMacros();
            ILProcessor processor = method.Body.GetILProcessor();
            int index = 0;
            int count = processor.Body.Instructions.Count;
            IReadOnlyList<BranchPoint> branchPoints = _cecilSymbolHelper.GetBranchPoints(method);
            System.Collections.Immutable.ImmutableArray<ReachabilityHelper.UnreachableRange> unreachableRanges = _reachabilityHelper.FindUnreachableIL(processor.Body.Instructions, processor.Body.ExceptionHandlers);
            int currentUnreachableRangeIx = 0;
            for (int n = 0; n < count; n++)
            {
                Instruction currentInstruction = processor.Body.Instructions[index];
                SequencePoint sequencePoint = method.DebugInformation.GetSequencePoint(currentInstruction);
                IEnumerable<BranchPoint> targetedBranchPoints = branchPoints.Where(p => p.EndOffset == currentInstruction.Offset);

                // make sure we're looking at the correct unreachable range (if any)
                int instrOffset = currentInstruction.Offset;
                while (currentUnreachableRangeIx < unreachableRanges.Length && instrOffset > unreachableRanges[currentUnreachableRangeIx].EndOffset)
                {
                    currentUnreachableRangeIx++;
                }

                // determine if the unreachable
                bool isUnreachable = false;
                if (currentUnreachableRangeIx < unreachableRanges.Length)
                {
                    ReachabilityHelper.UnreachableRange range = unreachableRanges[currentUnreachableRangeIx];
                    isUnreachable = instrOffset >= range.StartOffset && instrOffset <= range.EndOffset;
                }

                // Check is both reachable, _and_ coverable
                if (isUnreachable || _cecilSymbolHelper.SkipNotCoverableInstruction(method, currentInstruction))
                {
                    index++;
                    continue;
                }

                if (sequencePoint != null && !sequencePoint.IsHidden)
                {
                    if (_cecilSymbolHelper.SkipInlineAssignedAutoProperty(_parameters.SkipAutoProps, method, currentInstruction))
                    {
                        index++;
                        continue;
                    }

                    Instruction firstInjectedInstrumentedOpCode = AddInstrumentationCode(method, processor, currentInstruction, sequencePoint);
                    foreach (Instruction bodyInstruction in processor.Body.Instructions)
                        ReplaceInstructionTarget(bodyInstruction, currentInstruction, firstInjectedInstrumentedOpCode);

                    foreach (ExceptionHandler handler in processor.Body.ExceptionHandlers)
                        ReplaceExceptionHandlerBoundary(handler, currentInstruction, firstInjectedInstrumentedOpCode);

                    index += 2;
                }

                foreach (BranchPoint branchTarget in targetedBranchPoints)
                {
                    /*
                        * Skip branches with no sequence point reference for now.
                        * In this case for an anonymous class the compiler will dynamically create an Equals 'utility' method.
                        * The CecilSymbolHelper will create branch points with a start line of -1 and no document, which
                        * I am currently not sure how to handle.
                        */
                    if (branchTarget.StartLine == -1 || branchTarget.Document == null)
                        continue;

                    Instruction firstInjectedInstrumentedOpCode = AddInstrumentationCode(method, processor, currentInstruction, branchTarget);
                    foreach (Instruction bodyInstruction in processor.Body.Instructions)
                        ReplaceInstructionTarget(bodyInstruction, currentInstruction, firstInjectedInstrumentedOpCode);

                    foreach (ExceptionHandler handler in processor.Body.ExceptionHandlers)
                        ReplaceExceptionHandlerBoundary(handler, currentInstruction, firstInjectedInstrumentedOpCode);

                    index += 2;
                }

                index++;
            }

            method.Body.OptimizeMacros();
        }

        private Instruction AddInstrumentationCode(MethodDefinition method, ILProcessor processor, Instruction instruction, SequencePoint sequencePoint)
        {
            if (!_result.Documents.TryGetValue(_sourceRootTranslator.ResolveFilePath(sequencePoint.Document.Url), out Document document))
            {
                document = new Document { Path = _sourceRootTranslator.ResolveFilePath(sequencePoint.Document.Url) };
                document.Index = _result.Documents.Count;
                _result.Documents.Add(document.Path, document);
            }

            for (int i = sequencePoint.StartLine; i <= sequencePoint.EndLine; i++)
            {
                if (!document.Lines.ContainsKey(i))
                    document.Lines.Add(i, new Line { Number = i, Class = method.DeclaringType.FullName, Method = method.FullName });
            }

            _result.HitCandidates.Add(new HitCandidate(false, document.Index, sequencePoint.StartLine, sequencePoint.EndLine));

            return AddInstrumentationInstructions(method, processor, instruction, _result.HitCandidates.Count - 1);
        }

        private Instruction AddInstrumentationCode(MethodDefinition method, ILProcessor processor, Instruction instruction, BranchPoint branchPoint)
        {
            if (!_result.Documents.TryGetValue(_sourceRootTranslator.ResolveFilePath(branchPoint.Document), out Document document))
            {
                document = new Document { Path = _sourceRootTranslator.ResolveFilePath(branchPoint.Document) };
                document.Index = _result.Documents.Count;
                _result.Documents.Add(document.Path, document);
            }

            var key = new BranchKey(branchPoint.StartLine, (int)branchPoint.Ordinal);
            if (!document.Branches.ContainsKey(key))
            {
                document.Branches.Add(
                    key,
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

                if (method.DeclaringType.CustomAttributes.Any(x => x.AttributeType.FullName == typeof(CompilerGeneratedAttribute).FullName))
                {
                    if (_branchesInCompiledGeneratedClass == null)
                    {
                        _branchesInCompiledGeneratedClass = new List<string>();
                    }

                    if (!_branchesInCompiledGeneratedClass.Contains(method.FullName))
                    {
                        _branchesInCompiledGeneratedClass.Add(method.FullName);
                    }
                }
            }

            _result.HitCandidates.Add(new HitCandidate(true, document.Index, branchPoint.StartLine, (int)branchPoint.Ordinal));

            return AddInstrumentationInstructions(method, processor, instruction, _result.HitCandidates.Count - 1);
        }

        private Instruction AddInstrumentationInstructions(MethodDefinition method, ILProcessor processor, Instruction instruction, int hitEntryIndex)
        {
            if (_customTrackerRecordHitMethod == null)
            {
                string recordHitMethodName;
                if (_parameters.SingleHit)
                {
                    recordHitMethodName = _isCoreLibrary
                        ? nameof(ModuleTrackerTemplate.RecordSingleHitInCoreLibrary)
                        : nameof(ModuleTrackerTemplate.RecordSingleHit);
                }
                else
                {
                    recordHitMethodName = _isCoreLibrary
                        ? nameof(ModuleTrackerTemplate.RecordHitInCoreLibrary)
                        : nameof(ModuleTrackerTemplate.RecordHit);
                }

                _customTrackerRecordHitMethod = new MethodReference(
                    recordHitMethodName, method.Module.TypeSystem.Void, _customTrackerTypeDef);
                _customTrackerRecordHitMethod.Parameters.Add(new ParameterDefinition("hitLocationIndex", ParameterAttributes.None, method.Module.TypeSystem.Int32));
            }

            var indxInstr = Instruction.Create(OpCodes.Ldc_I4, hitEntryIndex);
            var callInstr = Instruction.Create(OpCodes.Call, _customTrackerRecordHitMethod);

            processor.InsertBefore(instruction, callInstr);
            processor.InsertBefore(callInstr, indxInstr);

            return indxInstr;
        }

        private static void ReplaceInstructionTarget(Instruction instruction, Instruction oldTarget, Instruction newTarget)
        {
            if (instruction.Operand is Instruction operandInstruction)
            {
                if (operandInstruction == oldTarget)
                {
                    instruction.Operand = newTarget;
                    return;
                }
            }
            else if (instruction.Operand is Instruction[] operandInstructions)
            {
                for (int i = 0; i < operandInstructions.Length; i++)
                {
                    if (operandInstructions[i] == oldTarget)
                        operandInstructions[i] = newTarget;
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

        private bool IsExcludeAttribute(CustomAttribute customAttribute)
        {
            return Array.IndexOf(_excludedAttributes, customAttribute.AttributeType.Name) != -1;
        }

        private static MethodBody GetMethodBody(MethodDefinition method)
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

        // Check if the member (type or method) is generated by the compiler from a method excluded from code coverage
        private bool IsSynthesizedMemberToBeExcluded(IMemberDefinition definition)
        {
            if (_excludedMethods is null)
            {
                return false;
            }

            TypeDefinition declaringType = definition.DeclaringType;

            // We check all parent type of current one bottom-up
            while (declaringType != null)
            {

                // If parent type is excluded return
                if (_excludedCompilerGeneratedTypes != null &&
                    _excludedCompilerGeneratedTypes.Any(t => t == declaringType.FullName))
                {
                    return true;
                }

                // Check methods members and compiler generated types
                foreach ((MethodDefinition, int) excludedMethods in _excludedMethods)
                {
                    // Exclude this member if declaring type is the same of the excluded method and 
                    // the name is synthesized from the name of the excluded method.
                    // 
                    if (declaringType.FullName == excludedMethods.Item1.DeclaringType.FullName &&
                        IsSynthesizedNameOf(definition.Name, excludedMethods.Item1.Name, excludedMethods.Item2))
                    {
                        return true;
                    }
                }
                declaringType = declaringType.DeclaringType;
            }

            return false;
        }

        // Check if the name is synthesized by the compiler
        // Refer to https://github.com/dotnet/roslyn/blob/master/src/Compilers/CSharp/Portable/Symbols/Synthesized/GeneratedNames.cs
        // to see how the compiler generate names for lambda, local function, yield or async/await expressions
        internal bool IsSynthesizedNameOf(string name, string methodName, int methodOrdinal)
        {
            return
                // Lambda method
                name.IndexOf($"<{methodName}>b__{methodOrdinal}") != -1 ||
                // Lambda display class
                name.IndexOf($"<>c__DisplayClass{methodOrdinal}_") != -1 ||
                // State machine
                name.IndexOf($"<{methodName}>d__{methodOrdinal}") != -1 ||
                // Local function
                (name.IndexOf($"<{methodName}>g__") != -1 && name.IndexOf($"|{methodOrdinal}_") != -1);
        }

        private static IEnumerable<string> CollectLambdaMethodsInsideLocalFunction(MethodDefinition methodDefinition)
        {
            if (!methodDefinition.Name.Contains(">g__")) yield break;

            foreach (Instruction instruction in methodDefinition.Body.Instructions.ToList())
            {
                if (instruction.OpCode == OpCodes.Ldftn && instruction.Operand is MethodReference mr && mr.Name.Contains(">b__"))
                {
                    yield return mr.FullName;
                }
            }
        }

        /// <summary>
        /// A custom importer created specifically to allow the instrumentation of System.Private.CoreLib by
        /// removing the external references to netstandard that are generated when instrumenting a typical
        /// assembly.
        /// </summary>
        private class CoreLibMetadataImporterProvider : IMetadataImporterProvider
        {
            public IMetadataImporter GetMetadataImporter(ModuleDefinition module)
            {
                return new CoreLibMetadataImporter(module);
            }

            private class CoreLibMetadataImporter : IMetadataImporter
            {
                private readonly ModuleDefinition _module;
                private readonly DefaultMetadataImporter _defaultMetadataImporter;

                public CoreLibMetadataImporter(ModuleDefinition module)
                {
                    _module = module;
                    _defaultMetadataImporter = new DefaultMetadataImporter(module);
                }

                public AssemblyNameReference ImportReference(AssemblyNameReference reference)
                {
                    return _defaultMetadataImporter.ImportReference(reference);
                }

                public TypeReference ImportReference(TypeReference type, IGenericParameterProvider context)
                {
                    TypeReference importedRef = _defaultMetadataImporter.ImportReference(type, context);
                    importedRef.GetElementType().Scope = _module.TypeSystem.CoreLibrary;
                    return importedRef;
                }

                public FieldReference ImportReference(FieldReference field, IGenericParameterProvider context)
                {
                    FieldReference importedRef = _defaultMetadataImporter.ImportReference(field, context);
                    importedRef.FieldType.GetElementType().Scope = _module.TypeSystem.CoreLibrary;
                    return importedRef;
                }

                public MethodReference ImportReference(MethodReference method, IGenericParameterProvider context)
                {
                    MethodReference importedRef = _defaultMetadataImporter.ImportReference(method, context);
                    importedRef.DeclaringType.GetElementType().Scope = _module.TypeSystem.CoreLibrary;

                    foreach (ParameterDefinition parameter in importedRef.Parameters)
                    {
                        if (parameter.ParameterType.Scope == _module.TypeSystem.CoreLibrary)
                        {
                            continue;
                        }

                        parameter.ParameterType.GetElementType().Scope = _module.TypeSystem.CoreLibrary;
                    }

                    if (importedRef.ReturnType.Scope != _module.TypeSystem.CoreLibrary)
                    {
                        importedRef.ReturnType.GetElementType().Scope = _module.TypeSystem.CoreLibrary;
                    }

                    return importedRef;
                }
            }
        }
    }

    // Exclude files helper https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.filesystemglobbing.matcher?view=aspnetcore-2.2
    internal class ExcludedFilesHelper
    {
        readonly Matcher _matcher;

        public ExcludedFilesHelper(string[] excludes, ILogger logger)
        {
            if (excludes != null && excludes.Length > 0)
            {
                _matcher = new Matcher();
                foreach (string excludeRule in excludes)
                {
                    if (excludeRule is null)
                    {
                        continue;
                    }
                    _matcher.AddInclude(Path.IsPathRooted(excludeRule) ? excludeRule.Substring(Path.GetPathRoot(excludeRule).Length) : excludeRule);
                }
            }
        }

        public bool Exclude(string sourceFile)
        {
            if (_matcher is null || sourceFile is null)
                return false;

            // We strip out drive because it doesn't work with globbing
            return _matcher.Match(Path.IsPathRooted(sourceFile) ? sourceFile.Substring(Path.GetPathRoot(sourceFile).Length) : sourceFile).HasMatches;
        }
    }
}
