// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Coverlet.Core
{
  internal class CoverageDetails
  {
#pragma warning disable IDE0032 // Use auto property
    private double _averageModulePercent;
#pragma warning restore IDE0032 // Use auto property

    public Modules Modules { get; internal set; }
    public double Covered { get; internal set; }
    public int Total { get; internal set; }
    public double Percent
    {
      get
      {
        if (Modules?.Count == 0) return 0;
        return Total == 0 ? 100D : Math.Floor((Covered / Total) * 10000) / 100;
      }
    }

    public double AverageModulePercent
    {
      get { return Math.Floor(_averageModulePercent * 100) / 100; }
      internal set { _averageModulePercent = value; }
    }
  }
}
