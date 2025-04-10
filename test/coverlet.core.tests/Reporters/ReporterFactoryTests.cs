﻿// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core.Reporters;
using Xunit;

namespace Coverlet.Core.Tests.Reporters
{
  public class ReporterFactoryTests
  {
    [Fact]
    public void TestCreateReporter()
    {
      Assert.Equal(typeof(JsonReporter), new ReporterFactory("json").CreateReporter().GetType());
      Assert.Equal(typeof(LcovReporter), new ReporterFactory("lcov").CreateReporter().GetType());
      Assert.Equal(typeof(OpenCoverReporter), new ReporterFactory("opencover").CreateReporter().GetType());
      Assert.Equal(typeof(CoberturaReporter), new ReporterFactory("cobertura").CreateReporter().GetType());
      Assert.Equal(typeof(TeamCityReporter), new ReporterFactory("teamcity").CreateReporter().GetType());
      Assert.Null(new ReporterFactory("").CreateReporter());
    }
  }
}
