using System;
using System.Collections.Generic;
using System.Text;
using Coverlet.Core.Symbols;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace coverlet.core.Abstractions
{
    internal interface ICecilSymbolHelper
    {
        List<BranchPoint> GetBranchPoints(MethodDefinition methodDefinition);

        bool SkipNotCoverableInstruction(MethodDefinition methodDefinition, Instruction instruction);
    }
}
