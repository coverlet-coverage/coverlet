// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Coverlet.Core.Abstractions;

namespace Coverlet.Core.Helpers
{
  internal class AssemblyAdapter : IAssemblyAdapter
  {
    public string GetAssemblyName(string assemblyPath)
    {
      return AssemblyName.GetAssemblyName(assemblyPath).Name;
    }
  }
}
