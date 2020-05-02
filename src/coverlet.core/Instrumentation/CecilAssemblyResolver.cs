using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Coverlet.Core.Abstractions;
using Coverlet.Core.Exceptions;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using Mono.Cecil;

namespace Coverlet.Core.Instrumentation
{
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
        private static readonly System.Reflection.Assembly _netStandardAssembly;
        private static readonly string _name;
        private static readonly byte[] _publicKeyToken;
        private static readonly AssemblyDefinition _assemblyDefinition;

        private readonly string _modulePath;
        private readonly Lazy<CompositeCompilationAssemblyResolver> _compositeResolver;
        private readonly ILogger _logger;

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

        public NetstandardAwareAssemblyResolver(string modulePath, ILogger logger)
        {
            _modulePath = modulePath;
            _logger = logger;

            // this is lazy because we cannot create AspNetCoreSharedFrameworkResolver if not on .NET Core runtime, 
            // runtime folders are different
            _compositeResolver = new Lazy<CompositeCompilationAssemblyResolver>(() => new CompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
            {
                new AppBaseCompilationAssemblyResolver(),
                new ReferenceAssemblyPathResolver(),
                new PackageCompilationAssemblyResolver(),
                new AspNetCoreSharedFrameworkResolver(_logger)
            }), true);
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
                try
                {
                    return base.Resolve(name);
                }
                catch (AssemblyResolutionException)
                {
                    AssemblyDefinition asm = TryWithCustomResolverOnDotNetCore(name);

                    if (asm != null)
                    {
                        return asm;
                    }

                    throw;
                }
            }
        }

        private bool IsDotNetCore()
        {
            // object for .NET Framework is inside mscorlib.dll
            return Path.GetFileName(typeof(object).Assembly.Location) == "System.Private.CoreLib.dll";
        }

        /// <summary>
        /// 
        /// We try to manually load assembly.
        /// To work test project needs to use
        ///
        /// <PropertyGroup>
        ///     <PreserveCompilationContext>true</PreserveCompilationContext>
        /// </PropertyGroup>
        /// 
        /// Runtime configuration file doc https://github.com/dotnet/cli/blob/master/Documentation/specs/runtime-configuration-file.md
        ///
        /// </summary>
        internal AssemblyDefinition TryWithCustomResolverOnDotNetCore(AssemblyNameReference name)
        {
            _logger.LogVerbose($"TryWithCustomResolverOnDotNetCore for {name}");

            if (!IsDotNetCore())
            {
                _logger.LogVerbose($"Not a dotnet core app");
                return null;
            }

            if (string.IsNullOrEmpty(_modulePath))
            {
                throw new AssemblyResolutionException(name);
            }

            using DependencyContextJsonReader contextJsonReader = new DependencyContextJsonReader();
            Dictionary<string, Lazy<AssemblyDefinition>> libraries = new Dictionary<string, Lazy<AssemblyDefinition>>();

            foreach (string fileName in Directory.GetFiles(Path.GetDirectoryName(_modulePath), "*.deps.json"))
            {
                using FileStream depsFile = File.OpenRead(fileName);
                _logger.LogVerbose($"Loading {fileName}");
                DependencyContext dependencyContext = contextJsonReader.Read(depsFile);
                foreach (CompilationLibrary library in dependencyContext.CompileLibraries)
                {
                    // we're interested only on nuget package
                    if (library.Type == "project")
                    {
                        continue;
                    }

                    try
                    {
                        string path = library.ResolveReferencePaths(_compositeResolver.Value).FirstOrDefault();
                        if (string.IsNullOrEmpty(path))
                        {
                            continue;
                        }

                        // We could load more than one deps file, we need to check if lib is already found
                        if (!libraries.ContainsKey(library.Name))
                        {
                            libraries.Add(library.Name, new Lazy<AssemblyDefinition>(() => AssemblyDefinition.ReadAssembly(path, new ReaderParameters() { AssemblyResolver = this })));
                        }
                    }
                    catch (Exception ex)
                    {
                        // if we don't find a lib go on
                        _logger.LogVerbose($"TryWithCustomResolverOnDotNetCore exception: {ex.ToString()}");
                    }
                }
            }

            if (libraries.TryGetValue(name.Name, out Lazy<AssemblyDefinition> asm))
            {
                return asm.Value;
            }

            throw new CecilAssemblyResolutionException($"AssemblyResolutionException for '{name}'. Try to add <PreserveCompilationContext>true</PreserveCompilationContext> to test projects </PropertyGroup> or pass '/p:CopyLocalLockFileAssemblies=true' option to the 'dotnet test' command-line", new AssemblyResolutionException(name));
        }
    }

    internal class AspNetCoreSharedFrameworkResolver : ICompilationAssemblyResolver
    {
        private readonly string[] _aspNetSharedFrameworkDirs = null;
        private readonly ILogger _logger = null;

        public AspNetCoreSharedFrameworkResolver(ILogger logger)
        {
            _logger = logger;
            string runtimeRootPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            string runtimeVersion = runtimeRootPath.Substring(runtimeRootPath.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            _aspNetSharedFrameworkDirs = new string[]
            {
               Path.GetFullPath(Path.Combine(runtimeRootPath,"../../Microsoft.AspNetCore.All", runtimeVersion)),
               Path.GetFullPath(Path.Combine(runtimeRootPath, "../../Microsoft.AspNetCore.App", runtimeVersion))
            };

            _logger.LogVerbose("AspNetCoreSharedFrameworkResolver search paths:");
            foreach (string searchPath in _aspNetSharedFrameworkDirs)
            {
                _logger.LogVerbose(searchPath);
            }
        }

        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
        {
            string dllName = $"{library.Name}.dll";

            foreach (string sharedFrameworkPath in _aspNetSharedFrameworkDirs)
            {
                if (!Directory.Exists(sharedFrameworkPath))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(sharedFrameworkPath))
                {
                    if (Path.GetFileName(file).Equals(dllName, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogVerbose($"'{dllName}' found in '{file}'");
                        assemblies.Add(file);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
