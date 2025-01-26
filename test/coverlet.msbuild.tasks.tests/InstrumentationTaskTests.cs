// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Symbols;
using Coverlet.MSbuild.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace coverlet.msbuild.tasks.tests
{
  public class InstrumentationTaskTests
  {
    private readonly Mock<IBuildEngine> _buildEngine;
    private readonly List<BuildErrorEventArgs> _errors;

    public InstrumentationTaskTests()
    {
      _buildEngine = new Mock<IBuildEngine>();
      _errors = new List<BuildErrorEventArgs>();
      _buildEngine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>())).Callback<BuildErrorEventArgs>(e => _errors.Add(e));
    }

    [Fact]
    public void Execute_StateUnderTest_Failure()
    {
      // Arrange
      var mockFileSystem = new Mock<IFileSystem>();
      mockFileSystem.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);

      var log = new TaskLoggingHelper(_buildEngine.Object, "InstrumentationTask");

      IServiceCollection serviceCollection = new ServiceCollection();
      serviceCollection.AddTransient<IFileSystem>(_ => mockFileSystem.Object);
      serviceCollection.AddTransient<IProcessExitHandler, ProcessExitHandler>();
      serviceCollection.AddTransient<IAssemblyAdapter, AssemblyAdapter>();
      serviceCollection.AddTransient<Coverlet.Core.Abstractions.ILogger, MSBuildLogger>(_ => new MSBuildLogger(log));
      serviceCollection.AddTransient<IRetryHelper, RetryHelper>();
      serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();
      serviceCollection.AddSingleton<ISourceRootTranslator, SourceRootTranslator>(serviceProvider => new SourceRootTranslator("testPath", serviceProvider.GetRequiredService<Coverlet.Core.Abstractions.ILogger>(), mockFileSystem.Object, new Mock<IAssemblyAdapter>().Object));
      serviceCollection.AddSingleton<ICecilSymbolHelper, CecilSymbolHelper>();

      ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
      BaseTask.ServiceProvider = serviceProvider;

      var instrumentationTask = new InstrumentationTask
      {
        Path = "testPath",
        BuildEngine = _buildEngine.Object
      };

      // Act
      bool success = instrumentationTask.Execute();

      // Assert
      Assert.False(success);
      Assert.NotEmpty(_errors);
      Assert.Contains("Module test path 'testPath' not found", _errors[0].Message);
    }
  }
}
