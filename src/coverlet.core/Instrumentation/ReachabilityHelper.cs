// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Coverlet.Core.Abstractions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Coverlet.Core.Instrumentation.Reachability
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
      => $"[IL_{StartOffset:x4}, IL_{EndOffset:x4}]";
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
      /// If an exception is raised in this block, where control might branch to.
      ///
      /// Note that this can happen even if the block's end is unreachable.
      /// </summary>
      public ImmutableArray<int> ExceptionBranchesTo { get; }

      /// <summary>
      /// Mutable, records whether control can flow into the block,
      /// ie. whether it's head is reachable
      /// </summary>
      public bool HeadReachable { get; set; }

      public BasicBlock(int startOffset, Instruction unreachableAfter, ImmutableArray<int> branchesTo, ImmutableArray<int> exceptionBranchesTo)
      {
        StartOffset = startOffset;
        UnreachableAfter = unreachableAfter;
        BranchesTo = branchesTo;
        ExceptionBranchesTo = exceptionBranchesTo;
      }

      public override string ToString()
      => $"{nameof(StartOffset)}=IL_{StartOffset:x4}, {nameof(HeadReachable)}={HeadReachable}, {nameof(TailReachable)}={TailReachable}, {nameof(BranchesTo)}=({string.Join(", ", BranchesTo.Select(b => $"IL_{b:x4}"))}), {nameof(ExceptionBranchesTo)}=({string.Join(", ", ExceptionBranchesTo.Select(b => $"IL_{b:x4}"))}), {nameof(UnreachableAfter)}={(UnreachableAfter != null ? $"IL_{UnreachableAfter:x4}" : "")}";
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

      /// <summary>
      /// Returns true if this branch has multiple targets.
      /// </summary>
      public bool HasMultiTargets => _targetOffset == -1;

      private readonly int _targetOffset;

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

          return _targetOffset;
        }
      }

#pragma warning disable IDE0032 // Use auto property

      private readonly ImmutableArray<int> _targetOffsets;
#pragma warning restore IDE0032 // Use auto property

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

          return _targetOffsets;
        }
      }

      public BranchInstruction(int offset, int targetOffset)
      {
        Offset = offset;
        _targetOffset = targetOffset;
        _targetOffsets = ImmutableArray<int>.Empty;
      }

      public BranchInstruction(int offset, ImmutableArray<int> targetOffset)
      {
        if (targetOffset.Length == 1)
        {
          throw new ArgumentException("Use single entry constructor for single targets", nameof(targetOffset));
        }

        Offset = offset;
        _targetOffset = -1;
        _targetOffsets = targetOffset;
      }

      public override string ToString()
      => $"IL_{Offset:x4}: {(HasMultiTargets ? string.Join(", ", TargetOffsets.Select(x => $"IL_{x:x4}")) : $"IL_{TargetOffset:x4}")}";
    }

    /// <summary>
    /// OpCodes that transfer control code, even if they do not
    /// introduce branch points.
    /// </summary>
    private static readonly ImmutableHashSet<OpCode> s_branchOpCodes =
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
                    OpCodes.Switch,

                    // These are forms of Br(_S) that are legal to use to exit
                    //   an exception block
                    //
                    // So they're "weird" but not too weird for our purposes
                    //
                    // The somewhat nasty subtlety is that, within an exception block,
                    //   it's perfectly legal to replace all normal branches with Leaves
                    //   even if they don't actually exit the block.
                    OpCodes.Leave,
                    OpCodes.Leave_S,

                    // these implicitly branch at the end of a filter or finally block
                    //   their operands do not encode anything interesting, we have to
                    //   look at exception handlers to figure out where they go to
                    OpCodes.Endfilter,
                    OpCodes.Endfinally
            }
        );

    /// <summary>
    /// OpCodes that unconditionally transfer control, so there
    /// is not "fall through" branch target.
    /// </summary>
    private static readonly ImmutableHashSet<OpCode> s_unconditionalBranchOpCodes =
        ImmutableHashSet.CreateRange(
            new[]
            {
                    OpCodes.Br,
                    OpCodes.Br_S,
                    OpCodes.Leave,
                    OpCodes.Leave_S
            }
        );

    private readonly ImmutableHashSet<MetadataToken> _doesNotReturnMethods;

    private ReachabilityHelper(ImmutableHashSet<MetadataToken> doesNotReturnMethods)
    {
      _doesNotReturnMethods = doesNotReturnMethods;
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

      ImmutableHashSet<MetadataToken> processedMethods = ImmutableHashSet<MetadataToken>.Empty;
      ImmutableHashSet<MetadataToken>.Builder doNotReturn = ImmutableHashSet.CreateBuilder<MetadataToken>();
      foreach (TypeDefinition type in module.Types)
      {
        foreach (MethodDefinition mtd in type.Methods)
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

          foreach (Instruction instr in body.Instructions)
          {
            if (!IsCall(instr, out MethodReference calledMtd))
            {
              continue;
            }

            MetadataToken token = calledMtd.MetadataToken;
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

            bool hasDoesNotReturnAttribute = false;
            foreach (CustomAttribute attr in mtdDef.CustomAttributes)
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

      ImmutableHashSet<MetadataToken> doNoReturnTokens = doNotReturn.ToImmutable();

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
    public ImmutableArray<UnreachableRange> FindUnreachableIL(Collection<Instruction> instrs, Collection<ExceptionHandler> exceptionHandlers)
    {
      // no instructions, means nothing to... not reach
      if (instrs.Count == 0)
      {
        return ImmutableArray<UnreachableRange>.Empty;
      }

      // no known methods that do not return, so everything is reachable by definition
      if (_doesNotReturnMethods.IsEmpty)
      {
        return ImmutableArray<UnreachableRange>.Empty;
      }

      (bool mayContainUnreachableCode, ImmutableArray<BranchInstruction> branches) = AnalyzeInstructions(instrs, exceptionHandlers);

      // no need to do any more work, nothing unreachable here
      if (!mayContainUnreachableCode)
      {
        return ImmutableArray<UnreachableRange>.Empty;
      }

      Instruction lastInstr = instrs[instrs.Count - 1];

      ImmutableArray<BasicBlock> blocks = CreateBasicBlocks(instrs, exceptionHandlers, branches);

      DetermineHeadReachability(blocks);
      return DetermineUnreachableRanges(blocks, lastInstr.Offset);
    }

    /// <summary>
    /// Analyzes the instructiona and exception handlers provided to find branches and determine if
    ///   it is possible for their to be unreachable code.
    /// </summary>
    private (bool MayContainUnreachableCode, ImmutableArray<BranchInstruction> Branches) AnalyzeInstructions(Collection<Instruction> instrs, Collection<ExceptionHandler> exceptionHandlers)
    {
      bool containsDoesNotReturnCall = false;

      ImmutableArray<BranchInstruction>.Builder ret = ImmutableArray.CreateBuilder<BranchInstruction>();
      foreach (Instruction i in instrs)
      {
        containsDoesNotReturnCall = containsDoesNotReturnCall || DoesNotReturn(i);

        if (s_branchOpCodes.Contains(i.OpCode))
        {
          (int? singleTargetOffset, ImmutableArray<int> multiTargetOffsets) = GetInstructionTargets(i, exceptionHandlers);

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

      return (containsDoesNotReturnCall, ret.ToImmutable());
    }

    /// <summary>
    /// For a single instruction, determines all the places it might branch to.
    /// </summary>
    private static (int? SingleTargetOffset, ImmutableArray<int> MultiTargetOffsets) GetInstructionTargets(Instruction i, Collection<ExceptionHandler> exceptionHandlers)
    {
      int? singleTargetOffset;
      ImmutableArray<int> multiTargetOffsets;

      if (i.Operand is Instruction[] multiTarget)
      {
        // it's a switch
        singleTargetOffset = null;

        multiTargetOffsets = ImmutableArray.Create(i.Next.Offset);
        foreach (Instruction instr in multiTarget)
        {
          // in practice these are small arrays, so a scan should be fine
          if (multiTargetOffsets.Contains(instr.Offset))
          {
            continue;
          }

          multiTargetOffsets = multiTargetOffsets.Add(instr.Offset);
        }
      }
      else if (i.Operand is Instruction targetInstr)
      {
        // it's any of the B.*(_S)? or Leave(_S)? instructions

        if (s_unconditionalBranchOpCodes.Contains(i.OpCode))
        {
          multiTargetOffsets = ImmutableArray<int>.Empty;
          singleTargetOffset = targetInstr.Offset;
        }
        else
        {
          singleTargetOffset = null;
          multiTargetOffsets = ImmutableArray.Create(i.Next.Offset, targetInstr.Offset);
        }
      }
      else if (i.OpCode == OpCodes.Endfilter)
      {
        // Endfilter is always the last instruction in a filter block, and no sort of control
        //   flow is allowed so we can scan backwards to see find the block

        ExceptionHandler filterForHandler = null;
        foreach (ExceptionHandler handler in exceptionHandlers)
        {
          if (handler.FilterStart == null)
          {
            continue;
          }

          Instruction startsAt = handler.FilterStart;
          Instruction cur = startsAt;
          while (cur != null && cur.Offset < i.Offset)
          {
            cur = cur.Next;
          }

          if (cur != null && cur.Offset == i.Offset)
          {
            filterForHandler = handler;
            break;
          }
        }

        if (filterForHandler == null)
        {
          throw new InvalidOperationException($"Could not find ExceptionHandler associated with {i}");
        }

        // filter can do one of two things:
        //   - branch into handler
        //   - percolate to another catch block, which might not be in this method
        //
        // so we chose to model this as an unconditional branch into the handler
        singleTargetOffset = filterForHandler.HandlerStart.Offset;
        multiTargetOffsets = ImmutableArray<int>.Empty;
      }
      else if (i.OpCode == OpCodes.Endfinally)
      {
        // Endfinally is very weird
        //
        // what it does, effectively is "take whatever branch would normally happen after the instruction
        //   that left the paired try
        //
        // practically, this makes endfinally a branch with no target

        singleTargetOffset = null;
        multiTargetOffsets = ImmutableArray<int>.Empty;
      }
      else
      {
        throw new InvalidOperationException($"Unexpected operand when processing branch {i}");
      }

      return (singleTargetOffset, multiTargetOffsets);
    }

    /// <summary>
    /// Calculates which ranges of IL are unreachable, given blocks which have head and tail reachability calculated.
    /// </summary>
    private static ImmutableArray<UnreachableRange> DetermineUnreachableRanges(ImmutableArray<BasicBlock> blocks, int lastInstructionOffset)
    {
      ImmutableArray<UnreachableRange>.Builder ret = ImmutableArray.CreateBuilder<UnreachableRange>();

      int endOfMethodOffset = lastInstructionOffset + 1; // add 1 so we point _past_ the end of the method

      for (int curBlockIx = 0; curBlockIx < blocks.Length; curBlockIx++)
      {
        BasicBlock curBlock = blocks[curBlockIx];

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
          Instruction doesNotReturnInstr = curBlock.UnreachableAfter;

          // and it's everything _after_ the following instruction that is unreachable
          // so record the following instruction through the end of the block
          Instruction followingInstr = doesNotReturnInstr.Next;

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
    private static void DetermineHeadReachability(ImmutableArray<BasicBlock> blocks)
    {
      var blockLookup = blocks.ToImmutableDictionary(b => b.StartOffset);

      BasicBlock headBlock = blockLookup[0];

      var knownLive = ImmutableStack.Create(headBlock);

      while (!knownLive.IsEmpty)
      {
        knownLive = knownLive.Pop(out BasicBlock block);

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
          foreach (int reachableOffset in block.BranchesTo)
          {
            BasicBlock reachableBlock = blockLookup[reachableOffset];
            knownLive = knownLive.Push(reachableBlock);
          }
        }

        // if the block is covered by an exception handler, then executing _any_ instruction in it
        //   could conceivably cause those handlers to be visited
        foreach (int exceptionHandlerOffset in block.ExceptionBranchesTo)
        {
          BasicBlock reachableHandler = blockLookup[exceptionHandlerOffset];
          knownLive = knownLive.Push(reachableHandler);
        }
      }
    }

    /// <summary>
    /// Create BasicBlocks from an instruction stream, exception blocks, and branches.
    ///
    /// Each block starts either at the start of the method, immediately after a branch or at a target for a branch,
    /// and ends with another branch, another branch target, or the end of the method.
    ///
    /// "Tail reachability" is also calculated, which is whether the block can ever actually get past its last instruction.
    /// </summary>
    private ImmutableArray<BasicBlock> CreateBasicBlocks(Collection<Instruction> instrs, Collection<ExceptionHandler> exceptionHandlers, ImmutableArray<BranchInstruction> branches)
    {
      // every branch-like instruction starts or stops a block
      ILookup<int, BranchInstruction> branchInstrLocs = branches.ToLookup(i => i.Offset);
      var branchInstrOffsets = branchInstrLocs.Select(k => k.Key).ToImmutableHashSet();

      // every target that might be branched to starts or stops a block
      ImmutableHashSet<int>.Builder branchTargetOffsetsBuilder = ImmutableHashSet.CreateBuilder<int>();
      foreach (BranchInstruction branch in branches)
      {
        if (branch.HasMultiTargets)
        {
          foreach (int target in branch.TargetOffsets)
          {
            branchTargetOffsetsBuilder.Add(target);
          }
        }
        else
        {
          branchTargetOffsetsBuilder.Add(branch.TargetOffset);
        }
      }

      // every exception handler an entry point
      //   either it's handler, or it's filter (if present)
      foreach (ExceptionHandler handler in exceptionHandlers)
      {
        if (handler.FilterStart != null)
        {
          branchTargetOffsetsBuilder.Add(handler.FilterStart.Offset);
        }
        else
        {
          branchTargetOffsetsBuilder.Add(handler.HandlerStart.Offset);
        }
      }

      ImmutableHashSet<int> branchTargetOffsets = branchTargetOffsetsBuilder.ToImmutable();

      // ending the method is also important
      int endOfMethodOffset = instrs[instrs.Count - 1].Offset;

      ImmutableArray<BasicBlock> blocks = ImmutableArray<BasicBlock>.Empty;
      int? blockStartedAt = null;
      Instruction unreachableAfter = null;
      foreach (Instruction i in instrs)
      {
        int offset = i.Offset;
        System.Collections.Generic.IEnumerable<BranchInstruction> branchesAtLoc = branchInstrLocs[offset];

        if (blockStartedAt == null)
        {
          blockStartedAt = offset;
          unreachableAfter = null;
        }

        bool isBranch = branchInstrOffsets.Contains(offset);
        bool isFollowedByBranchTarget = i.Next != null && branchTargetOffsets.Contains(i.Next.Offset);
        bool isEndOfMtd = endOfMethodOffset == offset;

        if (unreachableAfter == null && DoesNotReturn(i))
        {
          unreachableAfter = i;
        }

        bool blockEnds = isBranch || isFollowedByBranchTarget || isEndOfMtd;
        if (blockEnds)
        {
          Instruction nextInstr = i.Next;

          // figure out all the different places the basic block could lead to
          ImmutableArray<int> goesTo;
          if (branchesAtLoc.Any())
          {
            // it ends in a branch, where all does it branch?
            goesTo = ImmutableArray<int>.Empty;
            foreach (BranchInstruction branch in branchesAtLoc)
            {
              if (branch.HasMultiTargets)
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

          ImmutableArray<int> exceptionSwitchesTo = ImmutableArray<int>.Empty;

          // if the block is covered by any exception handlers then
          //   it is possible that it will branch to its handler block
          foreach (ExceptionHandler handler in exceptionHandlers)
          {
            int tryStart = handler.TryStart.Offset;
            int tryEnd = handler.TryEnd.Offset;

            bool containsStartOfTry =
                tryStart >= blockStartedAt.Value &&
                tryStart <= i.Offset;

            bool containsEndOfTry =
                tryEnd >= blockStartedAt.Value &&
                tryEnd <= i.Offset;

            bool blockInsideTry = blockStartedAt.Value >= tryStart && i.Offset <= tryEnd;

            // blocks do not necessarily align to the TRY part of exception handlers, so we need to handle three cases:
            //  - the try _starts_ in the block
            //  - the try _ends_ in the block
            //  - the try complete covers the block, but starts and ends before and after it (respectively)
            bool tryOverlapsBlock = containsStartOfTry || containsEndOfTry || blockInsideTry;

            if (!tryOverlapsBlock)
            {
              continue;
            }

            // if there's a filter, that runs first
            if (handler.FilterStart != null)
            {
              exceptionSwitchesTo = exceptionSwitchesTo.Add(handler.FilterStart.Offset);
            }
            else
            {
              // otherwise, go straight to the handler
              exceptionSwitchesTo = exceptionSwitchesTo.Add(handler.HandlerStart.Offset);
            }
          }

          blocks = blocks.Add(new BasicBlock(blockStartedAt.Value, unreachableAfter, goesTo, exceptionSwitchesTo));

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
      if (!IsCall(instr, out MethodReference mtd))
      {
        return false;
      }

      return _doesNotReturnMethods.Contains(mtd.MetadataToken);
    }

    /// <summary>
    /// Returns true if the given instruction is a Call or Callvirt.
    ///
    /// If it is a call, extracts the MethodReference that is being called.
    /// </summary>
    private static bool IsCall(Instruction instr, out MethodReference mtd)
    {
      OpCode opcode = instr.OpCode;
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
