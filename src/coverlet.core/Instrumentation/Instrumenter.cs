using System;
using System.Collections.Generic;
using System.Linq;

using Coverlet.Core.Helpers;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.Logging;

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

            Array.ForEach(_excludeFilters ?? Array.Empty<string>(), filter => _logger.LogInformation($"Excluded module filter '{filter}'"));
            Array.ForEach(_includeFilters ?? Array.Empty<string>(), filter => _logger.LogInformation($"Included module filter '{filter}'"));
            Array.ForEach(excludes ?? Array.Empty<string>(), filter => _logger.LogInformation($"Excluded source files '{filter}'"));

            _excludeFilters = _excludeFilters?.Where(f => InstrumentationHelper.IsValidFilterExpression(f)).ToArray();
            _includeFilters = _includeFilters?.Where(f => InstrumentationHelper.IsValidFilterExpression(f)).ToArray();

            foreach (var module in modules)
            {
                if (InstrumentationHelper.IsModuleExcluded(module, _excludeFilters) ||
                    !InstrumentationHelper.IsModuleIncluded(module, _includeFilters))
                {
                    _logger.LogInformation($"Excluded module: '{module}'");
                    continue;
                }

                var instrumenter = new ILInstrumenter(module, this._identifier, this._excludeFilters, this._includeFilters, excludes, this._excludeAttributes, this._singleHit, this._logger);
                if (instrumenter.CanInstrument())
                {
                    InstrumentationHelper.BackupOriginalModule(module, _identifier);

                    // Guard code path and restore if instrumentation fails.
                    try
                    {
                        var result = instrumenter.Instrument();
                        _results.Add(result);
                        _logger.LogInformation($"Instrumented module: '{module}'");
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
}
