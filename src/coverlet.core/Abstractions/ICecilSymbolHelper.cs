using System.Collections.Generic;
using Coverlet.Core.Symbols;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Coverlet.Core.Abstractions
{
    internal interface ICecilSymbolHelper
    {
        IReadOnlyList<BranchPoint> GetBranchPoints(MethodDefinition methodDefinition);
        bool SkipNotCoverableInstruction(MethodDefinition methodDefinition, Instruction instruction);
        bool SkipInlineAssignedAutoProperty(bool skipAutoProps, MethodDefinition methodDefinition, Instruction instruction);
    }
}
