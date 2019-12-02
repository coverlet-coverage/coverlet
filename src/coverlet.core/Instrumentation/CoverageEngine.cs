#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Coverlet.Core.Abstracts;
using Coverlet.Core.Helpers;
using Coverlet.Core.ObjectModel;
using Microsoft.Extensions.DependencyInjection;

namespace Coverlet.Core.Instrumentation
{
    public class BuildInCoverageEngineFactory : ICoverageEngineFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public BuildInCoverageEngineFactory()
        {
            _serviceProvider = GetServiceProvider();
        }

        protected virtual IServiceProvider GetServiceProvider()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<IRetryHelper, RetryHelper>();
            serviceCollection.AddTransient<IProcessExitHandler, ProcessExitHandler>();
            serviceCollection.AddTransient<IFileSystem, FileSystem>();
            serviceCollection.AddTransient<ILogger, ConsoleLogger>();
            serviceCollection.AddTransient<IReporterFactory, ReporterFactory>();
            // We need to keep singleton/static semantics
            serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();

            return serviceCollection.BuildServiceProvider();
        }

        public ICoverageEngine CreateEngine(InstrumentationOptions options)
        {
            return new CoverageEngine(options, _serviceProvider);
        }

        public IReporter CreateReporter(string format)
        {
            return _serviceProvider.GetService<IReporterFactory>().Create(format);
        }

        public IInProcessCoverageEngine CreateInProcessEngine(Stream instrumentationResultStream)
        {
            return new InProcessCoverageEngine(instrumentationResultStream);
        }
    }

    class CoverageEngine : ICoverageEngine
    {
        private readonly InstrumentationOptions _options;
        private readonly IServiceProvider _serviceProvider;

        public CoverageEngine(InstrumentationOptions? options, IServiceProvider? serviceProvider)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public CoverageResult GetCoverageResult(Stream stream)
        {
            Coverage coverage = new Coverage(
            CoveragePrepareResult.Deserialize(stream),
            _serviceProvider.GetService<ILogger>(),
            _serviceProvider.GetService<IInstrumentationHelper>(),
            _serviceProvider.GetService<IFileSystem>());

            return coverage.GetCoverageResult();
        }

        public Stream PrepareModules()
        {
            if (string.IsNullOrEmpty(_options.Module))
            {
                throw new ArgumentException("Module cannot be empty");
            }

            if (!File.Exists(_options.Module))
            {
                throw new FileNotFoundException($"Invalid module '{_options.Module}'");
            }

            Coverage coverage = new Coverage(
            _options.Module,
            _options.IncludeFilters,
            _options.IncludeDirectories,
            _options.ExcludeFilters,
            _options.ExcludeSourceFiles,
            _options.ExcludeAttributes,
            _options.IncludeTestAssembly,
            _options.SingleHit,
            _options.MergeWith,
            _options.UseSourceLink,
            _serviceProvider.GetService<ILogger>(),
            _serviceProvider.GetService<IInstrumentationHelper>(),
            _serviceProvider.GetService<IFileSystem>());

            CoveragePrepareResult result = coverage.PrepareModules();

            if (result.Results.Length == 0)
            {
                throw new InvalidOperationException("No module instrumented");
            }

            return CoveragePrepareResult.Serialize(result);
        }
    }

    class InProcessCoverageEngine : IInProcessCoverageEngine
    {
        private readonly Stream _instrumentationResultStream;

        public InProcessCoverageEngine(Stream instrumentationResultStream) => _instrumentationResultStream = instrumentationResultStream;

        public Assembly[] GetInstrumentedAssemblies()
        {
            HashSet<Assembly> instrumentedAssemblies = new HashSet<Assembly>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.Namespace == "Coverlet.Core.Instrumentation.Tracker"
                        && type.Name.StartsWith(assembly.GetName().Name + "_"))
                    {
                        instrumentedAssemblies.Add(assembly);
                    }
                }
            }
            return instrumentedAssemblies.ToArray();
        }

        public CoverageResult? ReadCurrentCoverage()
        {
            CoveragePrepareResult prepareResult = CoveragePrepareResult.Deserialize(_instrumentationResultStream);
            _instrumentationResultStream.Seek(0, SeekOrigin.Begin);

            Dictionary<string, Assembly> asmList = new Dictionary<string, Assembly>();
            foreach (Assembly asm in GetInstrumentedAssemblies())
            {
                asmList.Add(Path.GetFileNameWithoutExtension(asm.ManifestModule.ScopeName), asm);
            }

            return Coverage.GetCoverageResult(
                prepareResult.Results,
                prepareResult.UseSourceLink,
                new ConsoleLogger(),
                new InProcessFileSystem(prepareResult, asmList),
                new InProcessInstrumentationHelper(),
                prepareResult.Identifier,
                prepareResult.MergeWith);
        }

        // InProcess custom services

        class InProcessFileSystem : IFileSystem
        {
            private readonly Dictionary<string, Assembly> _asmList;
            private readonly CoveragePrepareResult _coveragePrepareResult;


            public InProcessFileSystem(CoveragePrepareResult coveragePrepareResult, Dictionary<string, Assembly> asmList) => (_asmList, _coveragePrepareResult) = (asmList, coveragePrepareResult);

            public void Copy(string sourceFileName, string destFileName, bool overwrite)
            {
                throw new NotImplementedException();
            }

            public void Delete(string path)
            {
                throw new NotImplementedException();
            }

            public bool Exists(string path)
            {
                // File does't exists until end of process or until flush
                return true;
            }

            public Stream NewFileStream(string path, FileMode mode)
            {
                foreach (InstrumenterResult result in _coveragePrepareResult.Results)
                {
                    if (result.HitsFilePath == path)
                    {
                        Assembly asm = _asmList[result.Module];
                        foreach (Type type in asm.GetTypes())
                        {
                            if (type.Namespace == "Coverlet.Core.Instrumentation.Tracker"
                                && type.Name.StartsWith(asm.GetName().Name + "_"))
                            {

                                Debug.Assert((string)type.GetField("HitsFilePath").GetValue(null) == path);

                                int[] hitsArray = (int[])type.GetField("HitsArray").GetValue(null);
                                MemoryStream ms = new MemoryStream();
                                using BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8, true);
                                bw.Write(hitsArray.Length);
                                for (int i = 0; i < hitsArray.Length; i++)
                                {
                                    bw.Write(hitsArray[i]);
                                }
                                ms.Seek(0, SeekOrigin.Begin);
                                return ms;
                            }
                        }
                    }
                }
                throw new InvalidOperationException($"Hits file '{path}' not found in results list");
            }

            public Stream NewFileStream(string path, FileMode mode, FileAccess access)
            {
                throw new NotImplementedException();
            }

            public Stream OpenRead(string path)
            {
                throw new NotImplementedException();
            }

            public string ReadAllText(string path)
            {
                throw new NotImplementedException();
            }

            public void WriteAllText(string path, string contents)
            {
                throw new NotImplementedException();
            }
        }

        class InProcessInstrumentationHelper : IInstrumentationHelper
        {
            public void BackupOriginalModule(string module, string identifier)
            {
                throw new NotImplementedException();
            }

            public bool DeleteHitsFile(string path)
            {
                // In process we don't remove files
                return false;
            }

            public bool EmbeddedPortablePdbHasLocalSource(string module, out string firstNotFoundDocument)
            {
                throw new NotImplementedException();
            }

            public string[] GetCoverableModules(string module, string[] directories, bool includeTestAssembly)
            {
                throw new NotImplementedException();
            }

            public bool HasPdb(string module, out bool embedded)
            {
                throw new NotImplementedException();
            }

            public bool IsLocalMethod(string method)
            {
                throw new NotImplementedException();
            }

            public bool IsModuleExcluded(string module, string[] excludeFilters)
            {
                throw new NotImplementedException();
            }

            public bool IsModuleIncluded(string module, string[] includeFilters)
            {
                throw new NotImplementedException();
            }

            public bool IsTypeExcluded(string module, string type, string[] excludeFilters)
            {
                throw new NotImplementedException();
            }

            public bool IsTypeIncluded(string module, string type, string[] includeFilters)
            {
                throw new NotImplementedException();
            }

            public bool IsValidFilterExpression(string filter)
            {
                throw new NotImplementedException();
            }

            public bool PortablePdbHasLocalSource(string module, out string firstNotFoundDocument)
            {
                throw new NotImplementedException();
            }

            public void RestoreOriginalModule(string module, string identifier)
            {
                // In process we don't restore nothing
            }
        }
    }

    // Plain vanilla console logger
    class ConsoleLogger : ILogger
    {
        public void LogError(string message)
        {
            Console.WriteLine($"[Error] {message}");
        }

        public void LogError(Exception exception)
        {
            Console.WriteLine($"[Error] {exception.ToString()}");
        }

        public void LogInformation(string message, bool important = false)
        {
            Console.WriteLine($"[LogInformation] {message}");
        }

        public void LogVerbose(string message)
        {
            Console.WriteLine($"[LogVerbose] {message}");
        }

        public void LogWarning(string message)
        {
            Console.WriteLine($"[LogWarning] {message}");
        }
    }
}

