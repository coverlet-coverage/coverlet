// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Exceptions;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using Mono.Cecil;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;

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
    private static readonly System.Reflection.Assembly s_netStandardAssembly;
    private static readonly string s_name;
    private static readonly byte[] s_publicKeyToken;
    private static readonly AssemblyDefinition s_assemblyDefinition;

    private readonly string _modulePath;
    private readonly Lazy<CompositeCompilationAssemblyResolver> _compositeResolver;
    private readonly ILogger _logger;

    static NetstandardAwareAssemblyResolver()
    {
      try
      {
        // To be sure to load information of "real" runtime netstandard implementation
        s_netStandardAssembly = System.Reflection.Assembly.LoadFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "netstandard.dll"));
        System.Reflection.AssemblyName name = s_netStandardAssembly.GetName();
        s_name = name.Name;
        s_publicKeyToken = name.GetPublicKeyToken();
        s_assemblyDefinition = AssemblyDefinition.ReadAssembly(s_netStandardAssembly.Location);
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

      // this is lazy because we cannot create NetCoreSharedFrameworkResolver if not on .NET Core runtime,
      // runtime folders are different
      _compositeResolver = new Lazy<CompositeCompilationAssemblyResolver>(() => new CompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
      {
                new NetCoreSharedFrameworkResolver(modulePath, _logger),
                new AppBaseCompilationAssemblyResolver(),
                new PackageCompilationAssemblyResolver(),
                new ReferenceAssemblyPathResolver(),
      }), true);
    }

    // Check name and public key but not version that could be different
    private static bool CheckIfSearchingNetstandard(AssemblyNameReference name)
    {
      if (s_netStandardAssembly is null)
      {
        return false;
      }

      if (s_name != name.Name)
      {
        return false;
      }

      if (name.PublicKeyToken.Length != s_publicKeyToken.Length)
      {
        return false;
      }

      for (int i = 0; i < name.PublicKeyToken.Length; i++)
      {
        if (s_publicKeyToken[i] != name.PublicKeyToken[i])
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
        return s_assemblyDefinition;
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

    private static bool IsDotNetCore()
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

      using var contextJsonReader = new DependencyContextJsonReader();
      var libraries = new Dictionary<string, Lazy<AssemblyDefinition>>();

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
            _logger.LogVerbose($"TryWithCustomResolverOnDotNetCore exception: {ex}");
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

  internal class NetCoreSharedFrameworkResolver : ICompilationAssemblyResolver
  {
    private readonly List<string> _aspNetSharedFrameworkDirs = new();
    private readonly ILogger _logger;

    public NetCoreSharedFrameworkResolver(string modulePath, ILogger logger)
    {
      _logger = logger;

      string runtimeConfigFile = Path.Combine(
          Path.GetDirectoryName(modulePath)!,
          Path.GetFileNameWithoutExtension(modulePath) + ".runtimeconfig.json");
      if (!File.Exists(runtimeConfigFile))
      {
        return;
      }

      var reader = new RuntimeConfigurationReader(runtimeConfigFile);
      IEnumerable<(string Name, string Version)> referencedFrameworks = reader.GetFrameworks();
      string runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location);
      string runtimeRootPath = Path.GetFullPath(Path.Combine(runtimePath!, "..", ".."));
      foreach ((string frameworkName, string frameworkVersion) in referencedFrameworks)
      {
        var semVersion = NuGetVersion.Parse(frameworkVersion);
        var directory = new DirectoryInfo(Path.Combine(runtimeRootPath, frameworkName));
        string majorVersion = $"{semVersion.Major}.{semVersion.Minor}.";
#pragma warning disable IDE0057 // Use range operator
        uint latestVersion = directory.GetDirectories().Where(x => x.Name.StartsWith(majorVersion))
            .Select(x => Convert.ToUInt32(x.Name.Substring(majorVersion.Length))).Max();
#pragma warning restore IDE0057 // Use range operator
        _aspNetSharedFrameworkDirs.Add(Directory.GetDirectories(directory.FullName, majorVersion + $"{latestVersion}*", SearchOption.TopDirectoryOnly)[0]);
      }

      _logger.LogVerbose("NetCoreSharedFrameworkResolver search paths:");
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

        string[] files = Directory.GetFiles(sharedFrameworkPath);
        foreach (string file in files)
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

  internal class RuntimeConfigurationReader
  {
    private readonly string _runtimeConfigFile;

    public RuntimeConfigurationReader(string runtimeConfigFile)
    {
      _runtimeConfigFile = runtimeConfigFile;
    }

    public IEnumerable<(string Name, string Version)> GetFrameworks()
    {
      string jsonString = File.ReadAllText(_runtimeConfigFile);

      var jsonLoadSettings = new JsonLoadSettings()
      {
        CommentHandling = CommentHandling.Ignore
      };

      var configuration = JObject.Parse(jsonString, jsonLoadSettings);

      JToken rootElement = configuration.Root;
      JToken runtimeOptionsElement = rootElement["runtimeOptions"];

      if (runtimeOptionsElement?["framework"] != null)
      {
        return new[] { (runtimeOptionsElement["framework"]["name"]?.Value<string>(), runtimeOptionsElement["framework"]["version"]?.Value<string>()) };
      }

      if (runtimeOptionsElement?["frameworks"] != null)
      {
        return runtimeOptionsElement["frameworks"].Select(x => (x["name"]?.Value<string>(), x["version"]?.Value<string>())).ToList();
      }

      if (runtimeOptionsElement?["includedFrameworks"] != null)
      {
        return runtimeOptionsElement["includedFrameworks"].Select(x => (x["name"]?.Value<string>(), x["version"]?.Value<string>())).ToList();
      }

      throw new InvalidOperationException($"Unable to read runtime configuration from {_runtimeConfigFile}.");
    }
  }
}
