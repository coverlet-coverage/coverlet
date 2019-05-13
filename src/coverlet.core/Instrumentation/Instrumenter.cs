using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

using Coverlet.Core.Attributes;
using Coverlet.Core.Helpers;
using Coverlet.Core.Logging;
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
        private readonly string[] _excludeFilters;
        private readonly string[] _includeFilters;
        private readonly string[] _excludedFiles;
        private readonly string[] _excludedAttributes;
        private readonly bool _singleHit;
        private readonly bool _isCoreLibrary;
        private readonly ILogger _logger;
        private InstrumenterResult _result;
        private FieldDefinition _customTrackerHitsArray;
        private FieldDefinition _customTrackerHitsFilePath;
        private FieldDefinition _customTrackerSingleHit;
        private ILProcessor _customTrackerClassConstructorIl;
        private TypeDefinition _customTrackerTypeDef;
        private MethodReference _customTrackerRegisterUnloadEventsMethod;
        private MethodReference _customTrackerRecordHitMethod;
        private List<string> _asyncMachineStateMethod;

        public Instrumenter(string module, string identifier, string[] excludeFilters, string[] includeFilters, string[] excludedFiles, string[] excludedAttributes, bool singleHit, ILogger logger)
        {
            _module = module;
            _identifier = identifier;
            _excludeFilters = excludeFilters;
            _includeFilters = includeFilters;
            _excludedFiles = excludedFiles ?? Array.Empty<string>();
            _excludedAttributes = excludedAttributes;
            _singleHit = singleHit;
            _isCoreLibrary = Path.GetFileNameWithoutExtension(_module) == "System.Private.CoreLib";
            _logger = logger;
        }

        public bool CanInstrument()
        {
            try
            {
                return InstrumentationHelper.HasPdb(_module);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Unable to instrument module: '{_module}' because : {ex.Message}");
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

            _result.AsyncMachineStateMethod = _asyncMachineStateMethod == null ? Array.Empty<string>() : _asyncMachineStateMethod.ToArray();

            return _result;
        }

        private void InstrumentModule()
        {
            using (var stream = new FileStream(_module, FileMode.Open, FileAccess.ReadWrite))
            using (var resolver = new NetstandardAwareAssemblyResolver())
            {
                resolver.AddSearchDirectory(Path.GetDirectoryName(_module));
                var parameters = new ReaderParameters { ReadSymbols = true, AssemblyResolver = resolver };
                if (_isCoreLibrary)
                {
                    parameters.MetadataImporterProvider = new CoreLibMetadataImporterProvider();
                }

                using (var module = ModuleDefinition.ReadModule(stream, parameters))
                {
                    var containsAppContext = module.GetType(nameof(System), nameof(AppContext)) != null;
                    var types = module.GetTypes();
                    AddCustomModuleTrackerToModule(module);

                    var sourceLinkDebugInfo = module.CustomDebugInformations.FirstOrDefault(c => c.Kind == CustomDebugInformationKind.SourceLink);
                    if (sourceLinkDebugInfo != null)
                    {
                        _result.SourceLink = ((SourceLinkDebugInformation)sourceLinkDebugInfo).Content;
                    }

                    foreach (TypeDefinition type in types)
                    {
                        var actualType = type.DeclaringType ?? type;
                        if (!actualType.CustomAttributes.Any(IsExcludeAttribute)
                            // Instrumenting Interlocked which is used for recording hits would cause an infinite loop.
                            && (!_isCoreLibrary || actualType.FullName != "System.Threading.Interlocked")
                            && !InstrumentationHelper.IsTypeExcluded(_module, actualType.FullName, _excludeFilters)
                            && InstrumentationHelper.IsTypeIncluded(_module, actualType.FullName, _includeFilters))
                            InstrumentType(type);
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
                    _customTrackerClassConstructorIl.InsertBefore(lastInstr, Instruction.Create(_singleHit ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
                    _customTrackerClassConstructorIl.InsertBefore(lastInstr, Instruction.Create(OpCodes.Stsfld, _customTrackerSingleHit));

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
                        var onProcessExitMethod = new MethodReference("OnProcessExit", module.TypeSystem.Void, appContextType).Resolve();
                        var onProcessExitIl = onProcessExitMethod.Body.GetILProcessor();

                        lastInstr = onProcessExitIl.Body.Instructions.Last();
                        onProcessExitIl.InsertBefore(lastInstr, Instruction.Create(OpCodes.Ldnull));
                        onProcessExitIl.InsertBefore(lastInstr, Instruction.Create(OpCodes.Ldnull));
                        onProcessExitIl.InsertBefore(lastInstr, Instruction.Create(OpCodes.Call, customTrackerUnloadModule));
                    }

                    module.Write(stream, new WriterParameters { WriteSymbols = true });
                }
            }
        }

        private void AddCustomModuleTrackerToModule(ModuleDefinition module)
        {
            using (AssemblyDefinition coverletInstrumentationAssembly = AssemblyDefinition.ReadAssembly(typeof(ModuleTrackerTemplate).Assembly.Location))
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
                }

                foreach (MethodDefinition methodDef in moduleTrackerTemplate.Methods)
                {
                    MethodDefinition methodOnCustomType = new MethodDefinition(methodDef.Name, methodDef.Attributes, methodDef.ReturnType);

                    foreach (var parameter in methodDef.Parameters)
                    {
                        methodOnCustomType.Parameters.Add(new ParameterDefinition(module.ImportReference(parameter.ParameterType)));
                    }

                    foreach (var variable in methodDef.Body.Variables)
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
                                foreach (var parameter in methodReference.Parameters)
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

                    foreach (var handler in methodDef.Body.ExceptionHandlers)
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

        private void InstrumentType(TypeDefinition type)
        {
            var methods = type.GetMethods();
            foreach (var method in methods)
            {
                MethodDefinition actualMethod = method;
                if (InstrumentationHelper.IsLocalMethod(method.Name))
                    actualMethod = methods.FirstOrDefault(m => m.Name == method.Name.Split('>')[0].Substring(1)) ?? method;

                if (!actualMethod.CustomAttributes.Any(IsExcludeAttribute))
                    InstrumentMethod(method);
            }

            var ctors = type.GetConstructors();
            foreach (var ctor in ctors)
            {
                if (!ctor.CustomAttributes.Any(IsExcludeAttribute))
                    InstrumentMethod(ctor);
            }
        }

        private void InstrumentMethod(MethodDefinition method)
        {
            var sourceFile = method.DebugInformation.SequencePoints.Select(s => s.Document.Url).FirstOrDefault();
            if (!string.IsNullOrEmpty(sourceFile) && _excludedFiles.Contains(sourceFile))
            {
                _logger.LogVerbose($"Excluded source file: '{sourceFile}'");
                return;
            }

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

                    index += 2;
                }

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

                    index += 2;
                }

                index++;
            }

            method.Body.OptimizeMacros();
        }

        private Instruction AddInstrumentationCode(MethodDefinition method, ILProcessor processor, Instruction instruction, SequencePoint sequencePoint)
        {
            if (!_result.Documents.TryGetValue(sequencePoint.Document.Url, out var document))
            {
                document = new Document { Path = sequencePoint.Document.Url };
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
            if (!_result.Documents.TryGetValue(branchPoint.Document, out var document))
            {
                document = new Document { Path = branchPoint.Document };
                document.Index = _result.Documents.Count;
                _result.Documents.Add(document.Path, document);
            }

            BranchKey key = new BranchKey(branchPoint.StartLine, (int)branchPoint.Ordinal);
            if (!document.Branches.ContainsKey(key))
            {
                document.Branches.Add(key,
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

                if (IsAsyncStateMachineBranch(method.DeclaringType, method))
                {
                    if (_asyncMachineStateMethod == null)
                    {
                        _asyncMachineStateMethod = new List<string>();
                    }

                    if (!_asyncMachineStateMethod.Contains(method.FullName))
                    {
                        _asyncMachineStateMethod.Add(method.FullName);
                    }
                }
            }

            _result.HitCandidates.Add(new HitCandidate(true, document.Index, branchPoint.StartLine, (int)branchPoint.Ordinal));

            return AddInstrumentationInstructions(method, processor, instruction, _result.HitCandidates.Count - 1);
        }

        private bool IsAsyncStateMachineBranch(TypeDefinition typeDef, MethodDefinition method)
        {
            if (!method.FullName.EndsWith("::MoveNext()"))
            {
                return false;
            }

            foreach (InterfaceImplementation implementedInterface in typeDef.Interfaces)
            {
                if (implementedInterface.InterfaceType.FullName == "System.Runtime.CompilerServices.IAsyncStateMachine")
                {
                    return true;
                }
            }
            return false;
        }

        private Instruction AddInstrumentationInstructions(MethodDefinition method, ILProcessor processor, Instruction instruction, int hitEntryIndex)
        {
            if (_customTrackerRecordHitMethod == null)
            {
                string recordHitMethodName;
                if (_singleHit)
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

        private bool IsExcludeAttribute(CustomAttribute customAttribute)
        {
            // The default custom attributes used to exclude from coverage.
            IEnumerable<string> excludeAttributeNames = new List<string>()
            {
                nameof(ExcludeFromCoverageAttribute),
                nameof(ExcludeFromCodeCoverageAttribute)
            };

            // Include the other attributes to exclude based on incoming parameters.
            if (_excludedAttributes != null)
            {
                excludeAttributeNames = _excludedAttributes.Union(excludeAttributeNames);
            }

            return excludeAttributeNames.Any(a =>
                customAttribute.AttributeType.Name.Equals(a.EndsWith("Attribute") ? a : $"{a}Attribute"));
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
                private readonly ModuleDefinition module;
                private readonly DefaultMetadataImporter defaultMetadataImporter;

                public CoreLibMetadataImporter(ModuleDefinition module)
                {
                    this.module = module;
                    this.defaultMetadataImporter = new DefaultMetadataImporter(module);
                }

                public AssemblyNameReference ImportReference(AssemblyNameReference reference)
                {
                    return this.defaultMetadataImporter.ImportReference(reference);
                }

                public TypeReference ImportReference(TypeReference type, IGenericParameterProvider context)
                {
                    var importedRef = this.defaultMetadataImporter.ImportReference(type, context);
                    importedRef.GetElementType().Scope = module.TypeSystem.CoreLibrary;
                    return importedRef;
                }

                public FieldReference ImportReference(FieldReference field, IGenericParameterProvider context)
                {
                    var importedRef = this.defaultMetadataImporter.ImportReference(field, context);
                    importedRef.FieldType.GetElementType().Scope = module.TypeSystem.CoreLibrary;
                    return importedRef;
                }

                public MethodReference ImportReference(MethodReference method, IGenericParameterProvider context)
                {
                    var importedRef = this.defaultMetadataImporter.ImportReference(method, context);
                    importedRef.DeclaringType.GetElementType().Scope = module.TypeSystem.CoreLibrary;

                    foreach (var parameter in importedRef.Parameters)
                    {
                        if (parameter.ParameterType.Scope == module.TypeSystem.CoreLibrary)
                        {
                            continue;
                        }

                        parameter.ParameterType.GetElementType().Scope = module.TypeSystem.CoreLibrary;
                    }

                    if (importedRef.ReturnType.Scope != module.TypeSystem.CoreLibrary)
                    {
                        importedRef.ReturnType.GetElementType().Scope = module.TypeSystem.CoreLibrary;
                    }

                    return importedRef;
                }
            }
        }
    }

    /// <summary>
    /// In case of testing different runtime i.e. netfx we could find netstandard.dll in folder.
    /// netstandard.dll is a forward only lib, there is no IL but only forwards to "runtime" implementation.
    /// For some classes implementation are in different assembly for different runtime for instance:
    /// 
    /// For NetFx 4.7
    /// // Token: 0x2700072C RID: 1836
    /// .class extern forwarder System.Security.Cryptography.X509Certificates.StoreName
    /// {
    ///    .assembly extern System
    /// }    
    /// 
    /// For netcoreapp2.2
    /// Token: 0x2700072C RID: 1836
    /// .class extern forwarder System.Security.Cryptography.X509Certificates.StoreName
    /// {
    ///    .assembly extern System.Security.Cryptography.X509Certificates
    /// }
    /// 
    /// There is a concrete possibility that Cecil cannot find implementation and throws StackOverflow exception https://github.com/jbevain/cecil/issues/575
    /// This custom resolver check if requested lib is a "official" netstandard.dll and load once of "current runtime" with
    /// correct forwards.
    /// Check compares 'assembly name' and 'public key token', because versions could differ between runtimes.
    /// </summary>
    internal class NetstandardAwareAssemblyResolver : DefaultAssemblyResolver
    {
        private static System.Reflection.Assembly _netStandardAssembly;
        private static string _name;
        private static byte[] _publicKeyToken;
        private static AssemblyDefinition _assemblyDefinition;

        static NetstandardAwareAssemblyResolver()
        {
            try
            {
                // To be sure to load information of "real" runtime netstandard implementation
                _netStandardAssembly = System.Reflection.Assembly.LoadFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "netstandard.dll"));
                System.Reflection.AssemblyName name = _netStandardAssembly.GetName();
                _name = name.Name;
                _publicKeyToken = name.GetPublicKeyToken();
                _assemblyDefinition = AssemblyDefinition.ReadAssembly(_netStandardAssembly.Location);
            }
            catch (FileNotFoundException)
            {
                // netstandard not supported
            }
        }

        // Check name and public key but not version that could be different
        private bool CheckIfSearchingNetstandard(AssemblyNameReference name)
        {
            if (_netStandardAssembly is null)
            {
                return false;
            }

            if (_name != name.Name)
            {
                return false;
            }

            if (name.PublicKeyToken.Length != _publicKeyToken.Length)
            {
                return false;
            }

            for (int i = 0; i < name.PublicKeyToken.Length; i++)
            {
                if (_publicKeyToken[i] != name.PublicKeyToken[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            if (CheckIfSearchingNetstandard(name))
            {
                return _assemblyDefinition;
            }
            else
            {
                return base.Resolve(name);
            }
        }
    }
}
