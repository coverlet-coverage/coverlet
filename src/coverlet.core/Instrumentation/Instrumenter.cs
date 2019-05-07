using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Coverlet.Core.Helpers;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.Logging;
using Mono.Cecil;

namespace Coverlet.Core
{
    public class Instrumenter : IInstrumenter
    {
        private string _module;
        private string[] _includeFilters;
        private string[] _includeDirectories;
        private string[] _excludeFilters;
        private string[] _excludedSourceFiles;
        private string[] _excludeAttributes;
        private bool _includeTestAssembly;
        private bool _singleHit;
        private string _mergeWith;
        private bool _useSourceLink;
        private ILogger _logger;
        private string _identifier;
        private List<InstrumenterResult> _results;

        public Instrumenter(string module,
            string[] includeFilters,
            string[] includeDirectories,
            string[] excludeFilters,
            string[] excludedSourceFiles,
            string[] excludeAttributes,
            bool includeTestAssembly,
            bool singleHit,
            string mergeWith,
            bool useSourceLink,
            ILogger logger)
        {
            _module = module;
            _includeFilters = includeFilters;
            _includeDirectories = includeDirectories ?? Array.Empty<string>();
            _excludeFilters = excludeFilters;
            _excludedSourceFiles = excludedSourceFiles;
            _excludeAttributes = excludeAttributes;
            _includeTestAssembly = includeTestAssembly;
            _singleHit = singleHit;
            _mergeWith = mergeWith;
            _useSourceLink = useSourceLink;
            _logger = logger;

            _identifier = Guid.NewGuid().ToString();
            _results = new List<InstrumenterResult>();
        }

        public InstrumenterState PrepareModules()
        {
            string[] modules = InstrumentationHelper.GetCoverableModules(_module, _includeDirectories, _includeTestAssembly);
            string[] excludes = InstrumentationHelper.GetExcludedFiles(_excludedSourceFiles);

            Array.ForEach(_excludeFilters ?? Array.Empty<string>(), filter => _logger.LogVerbose($"Excluded module filter '{filter}'"));
            Array.ForEach(_includeFilters ?? Array.Empty<string>(), filter => _logger.LogVerbose($"Included module filter '{filter}'"));
            Array.ForEach(excludes ?? Array.Empty<string>(), filter => _logger.LogVerbose($"Excluded source files '{filter}'"));

            _excludeFilters = _excludeFilters?.Where(f => InstrumentationHelper.IsValidFilterExpression(f)).ToArray();
            _includeFilters = _includeFilters?.Where(f => InstrumentationHelper.IsValidFilterExpression(f)).ToArray();

            foreach (var module in modules)
            {
                if (InstrumentationHelper.IsModuleExcluded(module, _excludeFilters) ||
                    !InstrumentationHelper.IsModuleIncluded(module, _includeFilters))
                {
                    _logger.LogVerbose($"Excluded module: '{module}'");
                    continue;
                }

                var instrumenter = new ILInstrumenter(module, this._identifier, this._excludeFilters, this._includeFilters, excludes, this._excludeAttributes, this._singleHit, this._logger);
                if (instrumenter.CanInstrument())
                {
                    InstrumentationHelper.BackupOriginalModule(module, _identifier);

                    // Guard code path and restore if instrumentation fails.
                    try
                    {
                        InstrumenterResult result = instrumenter.Instrument();
                        _results.Add(result);
                        _logger.LogVerbose($"Instrumented module: '{module}'");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Unable to instrument module: {module} because : {ex.Message}");
                        InstrumentationHelper.RestoreOriginalModule(module, _identifier);
                    }
                }
            }

            return new InstrumenterState()
            {
                Identifier = _identifier,
                InstrumenterResults = _results.ToArray(),
                MergeWith = _mergeWith,
                UseSourceLink = _useSourceLink
            };
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
