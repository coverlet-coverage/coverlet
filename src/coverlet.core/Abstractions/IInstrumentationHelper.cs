// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core.Enums;

namespace Coverlet.Core.Abstractions
{
  internal interface IInstrumentationHelper
  {
    void BackupOriginalModule(string module, string identifier);
    void DeleteHitsFile(string path);
    string[] GetCoverableModules(string module, string[] directories, bool includeTestAssembly);
    bool HasPdb(string module, out bool embedded);
    bool IsModuleExcluded(string module, string[] excludeFilters);
    bool IsModuleIncluded(string module, string[] includeFilters);
    bool IsValidFilterExpression(string filter);
    bool IsTypeExcluded(string module, string type, string[] excludeFilters);
    bool IsTypeIncluded(string module, string type, string[] includeFilters);
    void RestoreOriginalModule(string module, string identifier);
    bool EmbeddedPortablePdbHasLocalSource(string module, AssemblySearchType excludeAssembliesWithoutSources);
    bool PortablePdbHasLocalSource(string module, AssemblySearchType excludeAssembliesWithoutSources);
    bool IsLocalMethod(string method);
    void SetLogger(ILogger logger);
  }
}
