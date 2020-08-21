using Coverlet.Core.Abstractions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace coverlet.core.Instrumentation.Reachability
{
    /// <summary>
    /// Helper for find unreachable IL instructions.
    /// </summary>
    internal class ReachabilityHelper
    {
        internal readonly struct UnreachableRange
        {
            /// <summary>
            /// Offset of first unreachable instruction.
            /// </summary>
            public int StartOffset { get; }
            /// <summary>
            /// Offset of last unreachable instruction.
            /// </summary>
            public int EndOffset { get; }

            public UnreachableRange(int start, int end)
            {
                StartOffset = start;
                EndOffset = end;
            }

            public override string ToString()
            => $"[{StartOffset}, {EndOffset}]";
        }

        private class BasicBlock
        {
            /// <summary>
            /// Offset of the instruction that starts the block
            /// </summary>
            public int StartOffset { get; }
            /// <summary>
            /// Whether it is possible for control to flow past the end of the block,
            /// ie. whether it's tail is reachable
            /// </summary>
            public bool TailReachable => UnreachableAfter == null;
            /// <summary>
            /// If control flows to the end of the block, where it can flow to
            /// </summary>
            public ImmutableArray<int> BranchesTo { get; }

            /// <summary>
            /// If this block contains a call(i) to a method that does not return
            /// this will be the first such call.
            /// </summary>
            public Instruction UnreachableAfter { get; }

            /// <summary>
            /// Mutable, records whether control can flow into the block,
            /// ie. whether it's head is reachable
            /// </summary>
            public bool HeadReachable { get; set; }

            public BasicBlock(int startOffset, Instruction unreachableAfter, ImmutableArray<int> branchesTo)
            {
                StartOffset = startOffset;
                UnreachableAfter = unreachableAfter;
                BranchesTo = branchesTo;
            }

            public override string ToString()
            => $"{nameof(StartOffset)}={StartOffset}, {nameof(HeadReachable)}={HeadReachable}, {nameof(TailReachable)}={TailReachable}, {nameof(BranchesTo)}=({string.Join(", ", BranchesTo)}), {nameof(UnreachableAfter)}={UnreachableAfter}";
        }

        /// <summary>
        /// Represents an Instruction that transitions control flow (ie. branches).
        /// 
        /// This is _different_ from other branch types, like Branch and BranchPoint
        /// because it includes unconditional branches too.
        /// </summary>
        private readonly struct BranchInstruction
        {
            /// <summary>
            /// Location of the branching instruction
            /// </summary>
            public int Offset { get; }

            public bool HasMultiTargets => _TargetOffsets.Length > 0;

            private readonly int _TargetOffset;

            /// <summary>
            /// Target of the branch, assuming it has a single target.
            /// 
            /// It is illegal to access this if there are multiple targets.
            /// </summary>
            public int TargetOffset
            {
                get
                {
                    if (HasMultiTargets)
                    {
                        throw new InvalidOperationException($"{HasMultiTargets} is true");
                    }

                    return _TargetOffset;
                }
            }

            private readonly ImmutableArray<int> _TargetOffsets;

            /// <summary>
            /// Targets of the branch, assuming it has multiple targets.
            /// 
            /// It is illegal to access this if there is a single target.
            /// </summary>
            public ImmutableArray<int> TargetOffsets
            {
                get
                {
                    if (!HasMultiTargets)
                    {
                        throw new InvalidOperationException($"{HasMultiTargets} is false");
                    }

                    return _TargetOffsets;
                }
            }

            public BranchInstruction(int offset, int targetOffset)
            {
                Offset = offset;
                _TargetOffset = targetOffset;
                _TargetOffsets = ImmutableArray<int>.Empty;
            }

            public BranchInstruction(int offset, ImmutableArray<int> targetOffset)
            {
                if (targetOffset.Length < 1)
                {
                    throw new ArgumentException("Must have at least 2 entries", nameof(targetOffset));
                }

                Offset = offset;
                _TargetOffset = -1;
                _TargetOffsets = targetOffset;
            }
        }

        /// <summary>
        /// OpCodes that transfer control code, even if they do not
        /// introduce branch points.
        /// </summary>
        private static readonly ImmutableHashSet<OpCode> BRANCH_OPCODES =
            ImmutableHashSet.CreateRange(
                new[]
                {
                    OpCodes.Beq,
                    OpCodes.Beq_S,
                    OpCodes.Bge,
                    OpCodes.Bge_S,
                    OpCodes.Bge_Un,
                    OpCodes.Bge_Un_S,
                    OpCodes.Bgt,
                    OpCodes.Bgt_S,
                    OpCodes.Bgt_Un,
                    OpCodes.Bgt_Un_S,
                    OpCodes.Ble,
                    OpCodes.Ble_S,
                    OpCodes.Ble_Un,
                    OpCodes.Ble_Un_S,
                    OpCodes.Blt,
                    OpCodes.Blt_S,
                    OpCodes.Blt_Un,
                    OpCodes.Blt_Un_S,
                    OpCodes.Bne_Un,
                    OpCodes.Bne_Un_S,
                    OpCodes.Br,
                    OpCodes.Br_S,
                    OpCodes.Brfalse,
                    OpCodes.Brfalse_S,
                    OpCodes.Brtrue,
                    OpCodes.Brtrue_S,
                    OpCodes.Switch
                }
            );

        /// <summary>
        /// OpCodes that unconditionally transfer control, so there
        /// is not "fall through" branch target.
        /// </summary>
        private static readonly ImmutableHashSet<OpCode> UNCONDITIONAL_BRANCH_OPCODES =
            ImmutableHashSet.CreateRange(
                new[]
                {
                    OpCodes.Br,
                    OpCodes.Br_S
                }
            );

        private readonly ImmutableHashSet<MetadataToken> DoesNotReturnMethods;

        private ReachabilityHelper(ImmutableHashSet<MetadataToken> doesNotReturnMethods)
        {
            DoesNotReturnMethods = doesNotReturnMethods;
        }

        /// <summary>
        /// Build a ReachabilityHelper for the given module.
        /// 
        /// Predetermines methods that will not return, as 
        /// indicated by the presense of the given attributes.
        /// </summary>
        public static ReachabilityHelper CreateForModule(ModuleDefinition module, string[] doesNotReturnAttributes, ILogger logger)
        {
            if (doesNotReturnAttributes.Length == 0)
            {
                return new ReachabilityHelper(ImmutableHashSet<MetadataToken>.Empty);
            }

            var processedMethods = ImmutableHashSet<MetadataToken>.Empty;
            var doNotReturn = ImmutableHashSet.CreateBuilder<MetadataToken>();
            foreach (var type in module.Types)
            {
                foreach (var mtd in type.Methods)
                {
                    if (mtd.IsNative)
                    {
                        continue;
                    }

                    MethodBody body;
                    try
                    {
                        if (!mtd.HasBody)
                        {
                            continue;
                        }

                        body = mtd.Body;
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var instr in body.Instructions)
                    {
                        if (!IsCall(instr, out var calledMtd))
                        {
                            continue;
                        }

                        var token = calledMtd.MetadataToken;
                        if (processedMethods.Contains(token))
                        {
                            continue;
                        }

                        processedMethods = processedMethods.Add(token);

                        MethodDefinition mtdDef;
                        try
                        {
                            mtdDef = calledMtd.Resolve();
                        }
                        catch
                        {
                            logger.LogWarning($"Unable to resolve method reference \"{calledMtd.FullName}\", assuming calls to will return");
                            mtdDef = null;
                        }

                        if (mtdDef == null)
                        {
                            continue;
                        }

                        if (!mtdDef.HasCustomAttributes)
                        {
                            continue;
                        }

                        var hasDoesNotReturnAttribute = false;
                        foreach (var attr in mtdDef.CustomAttributes)
                        {
                            if (Array.IndexOf(doesNotReturnAttributes, attr.AttributeType.Name) != -1)
                            {
                                hasDoesNotReturnAttribute = true;
                                break;
                            }
                        }

                        if (hasDoesNotReturnAttribute)
                        {
                            logger.LogVerbose($"Determined call to \"{calledMtd.FullName}\" will not return");
                            doNotReturn.Add(token);
                        }
                    }
                }
            }

            var doNoReturnTokens = doNotReturn.ToImmutable();

            return new ReachabilityHelper(doNoReturnTokens);
        }

        /// <summary>
        /// Calculates which IL instructions are reachable given an instruction stream and branch points extracted from a method.
        /// 
        /// The algorithm works like so:
        ///  1. determine the "blocks" that make up a function
        ///     * A block starts with either the start of the method, or a branch _target_
        ///     * blocks are "where some other code might jump to"
        ///     * blocks end with either another branch, another branch target, or the end of the method
        ///     * this means blocks contain no control flow, except (maybe) as the very last instruction
        ///  2. blocks have head and tail reachability
        ///     * a block has head reachablility if some other block that is reachable can branch to it 
        ///     * a block has tail reachability if it contains no calls to methods that never return
        ///  4. push the first block onto a stack
        ///  5. while the stack is not empty
        ///     a. pop a block off the stack
        ///     b. give it head reachability
        ///     c. if the pop'd block is tail reachable, push the blocks it can branch to onto the stack
        ///  6. consider each block
        ///     * if it is head and tail reachable, all instructions in it are reachable
        ///     * if it is not head reachable (regardless of tail reachability), no instructions in it are reachable
        ///     * if it is only head reachable, all instructions up to and including the first call to a method that does not return are reachable
        /// </summary>
        public ImmutableArray<UnreachableRange> FindUnreachableIL(Collection<Instruction> instrs)
        {
            // no instructions, means nothing to... not reach
            if (instrs.Count == 0)
            {
                return ImmutableArray<UnreachableRange>.Empty;
            }

            // no known methods that do not return, so everything is reachable by definition
            if (DoesNotReturnMethods.IsEmpty)
            {
                return ImmutableArray<UnreachableRange>.Empty;
            }

            var brs = FindBranches(instrs);

            var lastInstr = instrs[instrs.Count - 1];

            var blocks = CreateBasicBlocks(instrs, brs);
            DetermineHeadReachability(blocks);
            return DetermineUnreachableRanges(blocks, lastInstr.Offset);
        }

        /// <summary>
        /// Discovers branches, including unconditional ones, in the given instruction stream.
        /// </summary>
        private ImmutableArray<BranchInstruction> FindBranches(Collection<Instruction> instrs)
        {
            var ret = ImmutableArray.CreateBuilder<BranchInstruction>();
            foreach (var i in instrs)
            {
                if (BRANCH_OPCODES.Contains(i.OpCode))
                {
                    int? singleTargetOffset;
                    ImmutableArray<int> multiTargetOffsets;

                    if (i.Operand is Instruction[] multiTarget)
                    {
                        // it's a switch
                        singleTargetOffset = null;

                        multiTargetOffsets = ImmutableArray.Create(i.Next.Offset);
                        foreach(var instr in multiTarget)
                        {
                            // in practice these are small arrays, so a scan should be fine
                            if(multiTargetOffsets.Contains(instr.Offset))
                            {
                                continue;
                            }

                            multiTargetOffsets = multiTargetOffsets.Add(instr.Offset);
                        }
                    }
                    else if (i.Operand is Instruction singleTarget)
                    {
                        // it's any of the B.*(_S)? instructions

                        if (UNCONDITIONAL_BRANCH_OPCODES.Contains(i.OpCode))
                        {
                            multiTargetOffsets = ImmutableArray<int>.Empty;
                            singleTargetOffset = singleTarget.Offset;
                        }
                        else
                        {
                            singleTargetOffset = null;
                            multiTargetOffsets = ImmutableArray.Create(i.Next.Offset, singleTarget.Offset);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected operand when processing branch {i.Operand}: {i.Operand}");
                    }

                    if (singleTargetOffset != null)
                    {
                        ret.Add(new BranchInstruction(i.Offset, singleTargetOffset.Value));
                    }
                    else
                    {
                        ret.Add(new BranchInstruction(i.Offset, multiTargetOffsets));
                    }
                }
            }

            return ret.ToImmutable();
        }

        /// <summary>
        /// Calculates which ranges of IL are unreachable, given blocks which have head and tail reachability calculated.
        /// </summary>
        private ImmutableArray<UnreachableRange> DetermineUnreachableRanges(ImmutableArray<BasicBlock> blocks, int lastInstructionOffset)
        {
            var ret = ImmutableArray.CreateBuilder<UnreachableRange>();

            var endOfMethodOffset = lastInstructionOffset + 1; // add 1 so we point _past_ the end of the method

            for (var curBlockIx = 0; curBlockIx < blocks.Length; curBlockIx++)
            {
                var curBlock = blocks[curBlockIx];

                int endOfCurBlockOffset;
                if (curBlockIx == blocks.Length - 1)
                {
                    endOfCurBlockOffset = endOfMethodOffset;
                }
                else
                {
                    endOfCurBlockOffset = blocks[curBlockIx + 1].StartOffset - 1;   // minus 1 so we don't include anything of the following block
                }

                if (curBlock.HeadReachable)
                {
                    if (curBlock.TailReachable)
                    {
                        // it's all reachable
                        continue;
                    }

                    // tail isn't reachable, which means there's a call to something that doesn't return...
                    var doesNotReturnInstr = curBlock.UnreachableAfter;

                    // and it's everything _after_ the following instruction that is unreachable
                    // so record the following instruction through the end of the block
                    var followingInstr = doesNotReturnInstr.Next;

                    ret.Add(new UnreachableRange(followingInstr.Offset, endOfCurBlockOffset));
                }
                else
                {
                    // none of it is reachable
                    ret.Add(new UnreachableRange(curBlock.StartOffset, endOfCurBlockOffset));
                }
            }

            return ret.ToImmutable();
        }

        /// <summary>
        /// Process all the blocks and determine if their first instruction is reachable,
        /// that is if they have "head reachability".
        /// 
        /// "Tail reachability" will have already been determined in CreateBlocks.
        /// </summary>
        private void DetermineHeadReachability(ImmutableArray<BasicBlock> blocks)
        {
            var blockLookup = blocks.ToImmutableDictionary(b => b.StartOffset);

            var headBlock = blockLookup[0];

            var knownLive = ImmutableStack.Create(headBlock);

            while (!knownLive.IsEmpty)
            {
                knownLive = knownLive.Pop(out var block);

                if (block.HeadReachable)
                {
                    // already seen this block
                    continue;
                }

                // we can reach this block, clearly
                block.HeadReachable = true;

                if (block.TailReachable)
                {
                    // we can reach all the blocks it might flow to
                    foreach (var reachableOffset in block.BranchesTo)
                    {
                        var reachableBlock = blockLookup[reachableOffset];
                        knownLive = knownLive.Push(reachableBlock);
                    }
                }
            }
        }

        /// <summary>
        /// Create BasicBlocks from an instruction stream and branches.
        /// 
        /// Each block starts either at the start of the method, immediately after a branch or at a target for a branch,
        /// and ends with another branch, another branch target, or the end of the method.
        /// 
        /// "Tail reachability" is also calculated, which is whether the block can ever actually get past its last instruction.
        /// </summary>
        private ImmutableArray<BasicBlock> CreateBasicBlocks(Collection<Instruction> instrs, ImmutableArray<BranchInstruction> branches)
        {
            // every branch-like instruction starts or stops a block
            var branchInstrLocs = branches.ToLookup(i => i.Offset);
            var branchInstrOffsets = branchInstrLocs.Select(k => k.Key).ToImmutableHashSet();

            // every target that might be branched to starts or stops a block
            var branchTargetOffsets = ImmutableHashSet<int>.Empty;
            foreach (var branch in branches)
            {
                if (branch.HasMultiTargets)
                {
                    foreach (var target in branch.TargetOffsets)
                    {
                        branchTargetOffsets = branchTargetOffsets.Add(target);
                    }
                }
                else
                {
                    branchTargetOffsets = branchTargetOffsets.Add(branch.TargetOffset);
                }
            }

            // ending the method is also important
            var endOfMethodOffset = instrs[instrs.Count - 1].Offset;

            var blocks = ImmutableArray<BasicBlock>.Empty;
            int? blockStartedAt = null;
            Instruction unreachableAfter = null;
            foreach (var i in instrs)
            {
                var offset = i.Offset;
                var branchesAtLoc = branchInstrLocs[offset];

                if (blockStartedAt == null)
                {
                    blockStartedAt = offset;
                    unreachableAfter = null;
                }

                var isBranch = branchInstrOffsets.Contains(offset);
                var isFollowedByBranchTarget = i.Next != null && branchTargetOffsets.Contains(i.Next.Offset);
                var isEndOfMtd = endOfMethodOffset == offset;

                if (unreachableAfter == null && DoesNotReturn(i))
                {
                    unreachableAfter = i;
                }

                var blockEnds = isBranch || isFollowedByBranchTarget || isEndOfMtd;
                if (blockEnds)
                {
                    var nextInstr = i.Next;

                    // figure out all the different places the basic block could lead to
                    ImmutableArray<int> goesTo;
                    if(branchesAtLoc.Any())
                    {
                        // it ends in a branch, where all does it branch?
                        goesTo = ImmutableArray<int>.Empty;
                        foreach(var branch in branchesAtLoc)
                        {
                            if(branch.HasMultiTargets)
                            {
                                goesTo = goesTo.AddRange(branch.TargetOffsets);
                            }
                            else
                            {
                                goesTo = goesTo.Add(branch.TargetOffset);
                            }
                        }
                    }
                    else if (nextInstr != null)
                    {
                        // it falls throw to another instruction
                        goesTo = ImmutableArray.Create(nextInstr.Offset);
                    }
                    else
                    {
                        // it ends the method
                        goesTo = ImmutableArray<int>.Empty;
                    }

                    blocks = blocks.Add(new BasicBlock(blockStartedAt.Value, unreachableAfter, goesTo));

                    blockStartedAt = null;
                    unreachableAfter = null;
                }
            }

            return blocks;
        }

        /// <summary>
        /// Returns true if the given instruction will never return, 
        /// and thus subsequent instructions will never be run.
        /// </summary>
        private bool DoesNotReturn(Instruction instr)
        {
            if (!IsCall(instr, out var mtd))
            {
                return false;
            }

            return DoesNotReturnMethods.Contains(mtd.MetadataToken);
        }

        /// <summary>
        /// Returns true if the given instruction is a Call or Callvirt.
        /// 
        /// If it is a call, extracts the MethodReference that is being called.
        /// </summary>
        private static bool IsCall(Instruction instr, out MethodReference mtd)
        {
            var opcode = instr.OpCode;
            if (opcode != OpCodes.Call && opcode != OpCodes.Callvirt)
            {
                mtd = null;
                return false;
            }

            mtd = (MethodReference)instr.Operand;

            return true;
        }
    }
}
