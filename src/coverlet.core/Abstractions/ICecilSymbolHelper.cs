// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Coverlet.Core.Symbols;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Coverlet.Core.Abstractions
{
  public interface ICecilSymbolHelper
  {
    IReadOnlyList<BranchPoint> GetBranchPoints(MethodDefinition methodDefinition);
    bool SkipNotCoverableInstruction(MethodDefinition methodDefinition, Instruction instruction);
    bool SkipInlineAssignedAutoProperty(bool skipAutoProps, MethodDefinition methodDefinition, Instruction instruction);
  }
}
