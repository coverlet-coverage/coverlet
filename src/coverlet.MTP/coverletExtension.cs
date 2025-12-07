// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Testing.Platform.Extensions;

namespace coverlet.Extension;

internal class CoverletExtension : IExtension
{
  public string Uid => nameof(CoverletExtension);

  public string DisplayName => "Coverlet Code Coverage Collector";

  public string Version => typeof(CoverletExtension).Assembly.GetName().Version?.ToString() ?? "1.0.0";

  public string Description => "Provides code coverage collection for the Microsoft Testing Platform";

  public Task<bool> IsEnabledAsync() => Task.FromResult(true);
}
