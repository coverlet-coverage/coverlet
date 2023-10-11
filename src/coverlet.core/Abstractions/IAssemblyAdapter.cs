// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Coverlet.Core.Abstractions
{
  internal interface IAssemblyAdapter
  {
    string GetAssemblyName(string assemblyPath);
  }
}
