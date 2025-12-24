// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Coverlet.Core.Instrumentation;
using Coverlet.MTP.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.Services;

namespace Coverlet.MTP.InProcDataCollection;

public class CoverletTestSessionHandler : ITestSessionLifetimeHandler
{
  private readonly CoverletMTPSettings _settings;

  public CoverletTestSessionHandler()
  {
    _settings = new CoverletMTPSettings();
  }

  public CoverletTestSessionHandler(IConfiguration? configuration, string testModule)
  {
    var parser = new CoverletMTPSettingsParser();
    _settings = parser.Parse(configuration, testModule);
  }

  public string Uid => nameof(CoverletTestSessionHandler);
  public string Version => "1.0.0";
  public string DisplayName => "Coverlet Coverage Session Handler";
  public string Description => "Flushes coverage data at end of test session";

  public Task<bool> IsEnabledAsync() => Task.FromResult(true);

  Task ITestSessionLifetimeHandler.OnTestSessionStartingAsync(ITestSessionContext testSessionContext)
  {
    return ((ITestSessionLifetimeHandler)this).OnTestSessionStartingAsync(testSessionContext);
  }

  Task ITestSessionLifetimeHandler.OnTestSessionFinishingAsync(ITestSessionContext testSessionContext)
  {
    return ((ITestSessionLifetimeHandler)this).OnTestSessionFinishingAsync(testSessionContext);
  }

  public Task OnTestSessionStartingAsync(ITestSessionContext testSessionContext)
  {
    throw new NotImplementedException();
  }

  public Task OnTestSessionFinishingAsync(ITestSessionContext testSessionContext)
  {
    throw new NotImplementedException();
  }

  private Task OnTestSessionStartingAsync_Impl(ITestSessionContext testSessionContext)
  {
    return Task.CompletedTask;
  }

  private Task OnTestSessionFinishingAsync_Impl(ITestSessionContext testSessionContext)
  {
    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
      Type? injectedInstrumentationClass = GetInstrumentationClass(assembly);
      if (injectedInstrumentationClass is null)
      {
        continue;
      }

      MethodInfo? unloadModule = injectedInstrumentationClass.GetMethod(
          nameof(ModuleTrackerTemplate.UnloadModule),
          [typeof(object), typeof(EventArgs)]);

      unloadModule?.Invoke(null, [this, EventArgs.Empty]);

      injectedInstrumentationClass
          .GetField("FlushHitFile", BindingFlags.Static | BindingFlags.Public)?
          .SetValue(null, false);
    }

    return Task.CompletedTask;
  }

  internal static Type? GetInstrumentationClass(Assembly assembly)
  {
    try
    {
      foreach (Type type in assembly.GetTypes())
      {
        if (type.Namespace == "Coverlet.Core.Instrumentation.Tracker"
            && type.Name.StartsWith(assembly.GetName().Name + "_"))
        {
          return type;
        }
      }

      return null;
    }
    catch (Exception)
    {
      return null;
    }
  }
}
