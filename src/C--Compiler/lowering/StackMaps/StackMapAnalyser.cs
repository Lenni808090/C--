namespace CMinus.Compiler.Lowering;

class StackMapAnalyser {


    Dictionary<int, BlockLiveness> BlockAnalyseFunction(IrFunction function) {
        Dictionary<int, BlockLiveness> live = new();
        Dictionary<int, BlockRefUseDef> useDefs = new();
        Dictionary<int, BasicBlock> idToBlock = new();

        foreach (BasicBlock block in function.basicBlocks) {
            idToBlock[block.blockId] = block;
            useDefs[block.blockId] = BuildBlockRefUse(block, function);
            live[block.blockId] = new BlockLiveness(function.maxVReg, function.localCount);
        }

        bool changed = true;

        while (changed) {
            changed = false;
            for (int i = function.basicBlocks.Length - 1; i >= 0; i--) {
                BasicBlock block = function.basicBlocks[i];
                BlockLiveness liveness = live[block.blockId];
                BlockRefUseDef blockUseDef = useDefs[block.blockId];

                bool[] newLiveOutRegs = new bool[function.maxVReg];
                bool[] newLiveOutLocals = new bool[function.localCount];

                foreach (BasicBlock succ in getSuccesors(block, idToBlock)) {
                    OrInto(newLiveOutRegs, live[succ.blockId].liveInRegs);
                    OrInto(newLiveOutLocals, live[succ.blockId].liveInLocals);
                }

                bool[] newLiveInRegs = new bool[function.maxVReg];
                bool[] newLiveInLocals = new bool[function.localCount];

                //works because only regs or locals that are not defined inside a block are added to used
                CopyInto(newLiveInRegs, blockUseDef.usedRegs);
                CopyInto(newLiveInLocals, blockUseDef.usedLocals);

                OrInto(newLiveInRegs, Subtract(newLiveOutRegs, blockUseDef.definedRegs));
                OrInto(newLiveInLocals, Subtract(newLiveOutLocals, blockUseDef.definedLocals));

                if (!IsEqual(newLiveInLocals, liveness.liveInLocals) || !IsEqual(newLiveInRegs, liveness.liveInRegs) || !IsEqual(newLiveOutLocals, liveness.liveOutLocals) || !IsEqual(newLiveOutRegs, liveness.liveOutRegs)) {
                    liveness.liveInLocals = newLiveInLocals;
                    liveness.liveInRegs = newLiveInRegs;
                    liveness.liveOutLocals = newLiveOutLocals;
                    liveness.liveOutRegs = newLiveOutRegs;

                    changed = true;
                }

            }
        }

        return live;
    }


    List<BasicBlock> getSuccesors(BasicBlock block, Dictionary<int, BasicBlock> idToBlock) {
        List<BasicBlock> result = new();
        switch (block.terminator) {
            case IrGoto @goto: {
                    result.Add(idToBlock[@goto.basicBlockId]);
                    break;
                }
            case IrBranch branch: {
                    result.Add(idToBlock[branch.thenBlockId]);
                    result.Add(idToBlock[branch.elseBlockId]);
                    break;
                }
            case IrReturn: {
                    break;
                }
            default: {
                    throw new Exception("unkown terminator in get succesor");
                }
        }
        return result;
    }

    BlockRefUseDef BuildBlockRefUse(BasicBlock block, IrFunction function) {
        var blockUseRef = new BlockRefUseDef(function.maxVReg, function.localCount);

        foreach (IrInstr instr in block.irInstrs) {
            RefUseDef res = GetRefUseInstr(instr, function);
            MergeIntoBlockUseDef(blockUseRef, res);
        }

        if (block.terminator is not null) {
            var res = GetRefUseDefTerminator(block.terminator, function);
            MergeIntoBlockUseDef(blockUseRef, res);
        }

        return blockUseRef;
    }


    void MergeIntoBlockUseDef(BlockRefUseDef block, RefUseDef instr) {
        // when the block uses the reg and didnt define it set true
        for (int i = 0; i < block.usedRegs.Length; i++) {
            if (instr.usedRegs[i] && !block.definedRegs[i]) {
                block.usedRegs[i] = true;
            }
            // if the instruction defined it the block has defined it
            if (instr.definedRegs[i]) {
                block.definedRegs[i] = true;
            }
        }

        //same logic as above for locals
        for (int i = 0; i < block.usedLocals.Length; i++) {
            if (instr.usedLocals[i] && !block.definedLocals[i]) {
                block.usedLocals[i] = true;
            }

            if (instr.definedLocals[i]) {
                block.definedLocals[i] = true;
            }
        }
    }

    RefUseDef GetRefUseDefTerminator(Terminator terminator, IrFunction irFunction) {
        var regIsRef = irFunction.regIsRef;

        bool[] usedRegs = new bool[irFunction.maxVReg];
        bool[] usedLocals = new bool[irFunction.localCount];
        bool[] definedRegs = new bool[irFunction.maxVReg];
        bool[] definedLocals = new bool[irFunction.localCount];

        switch (terminator) {
            case IrGoto: {
                    break;
                }
            case IrBranch branch: {
                    usedRegs[branch.condReg] = regIsRef[branch.condReg];
                    break;
                }
            case IrReturn @return: {
                    usedRegs[@return.returnReg] = regIsRef[@return.returnReg];
                    break;
                }
            default: {
                    throw new Exception("unknown terminator in stack analyser");
                }
        }

        return new RefUseDef(usedRegs, usedLocals, definedRegs, definedLocals);
    }

    RefUseDef GetRefUseInstr(IrInstr irInstr, IrFunction irFunction) {
        var regIsRef = irFunction.regIsRef;
        var localIsRef = irFunction.localIsRef;
        bool[] usedRegs = new bool[irFunction.maxVReg];
        bool[] usedLocals = new bool[irFunction.localCount];
        bool[] definedRegs = new bool[irFunction.maxVReg];
        bool[] definedLocals = new bool[irFunction.localCount];

        switch (irInstr) {
            case IrStoreLocal storeLocal: {
                    if (localIsRef[storeLocal.localIndex]) {
                        definedLocals[storeLocal.localIndex] = true;
                        usedRegs[storeLocal.srcReg] = true;
                    }
                    break;
                }
            case IrLoadConst: {
                    break;
                }
            case IrLoadLocal loadLocal: {
                    if (localIsRef[loadLocal.localIndex]) {
                        usedLocals[loadLocal.localIndex] = true;
                        definedRegs[loadLocal.dstReg] = true;
                    }
                    break;
                }
            case IrNewArray newArray: {
                    definedRegs[newArray.dstReg] = regIsRef[newArray.dstReg];
                    break;
                }
            case IrArrayLength arrayLength: {
                    usedRegs[arrayLength.arrayReg] = regIsRef[arrayLength.arrayReg];
                    break;
                }
            case IrStoreElement storeElement: {
                    usedRegs[storeElement.arrayReg] = regIsRef[storeElement.arrayReg];
                    usedRegs[storeElement.srcReg] = regIsRef[storeElement.srcReg];
                    break;
                }
            case IrLoadElement loadElement: {
                    if (regIsRef[loadElement.dstReg]) {
                        definedRegs[loadElement.dstReg] = true;
                    }
                    usedRegs[loadElement.arrayReg] = regIsRef[loadElement.arrayReg];
                    break;
                }
            case IrMove move: {
                    usedRegs[move.srcReg] = regIsRef[move.srcReg];
                    definedRegs[move.dstReg] = regIsRef[move.dstReg];
                    break;
                }
            case IrBinaryOp binaryOp: {
                    usedRegs[binaryOp.leftReg] = regIsRef[binaryOp.leftReg];
                    usedRegs[binaryOp.rightReg] = regIsRef[binaryOp.rightReg];
                    break;
                }
            case IrUnary unary: {
                    definedRegs[unary.dstReg] = regIsRef[unary.dstReg];
                    usedRegs[unary.operandReg] = regIsRef[unary.operandReg];
                    break;
                }
            case IrCall call: {
                    definedRegs[call.dstReg] = regIsRef[call.dstReg];
                    foreach (int reg in call.argRegs) {
                        usedRegs[reg] = regIsRef[reg];
                    }
                    break;
                }
            default: {
                    throw new Exception("unknown ir instruction in stack map analyser");
                }
        }

        return new RefUseDef(usedRegs, usedLocals, definedRegs, definedLocals);
    }


    void OrInto(bool[] left, bool[] right) {
        for (int i = 0; i < left.Length; i++) {
            left[i] |= right[i];
        }
    }

    bool[] Subtract(bool[] left, bool[] right) {
        for (int i = 0; i < left.Length; i++) {
            left[i] = left[i] && !right[i];
        }

        return left;
    }

    void CopyInto(bool[] left, bool[] right) {
        for (int i = 0; i < left.Length; i++) {
            left[i] = right[i];
        }
    }

    bool IsEqual(bool[] left, bool[] right) {
        for (int i = 0; i < left.Length; i++) {
            if (left[i] != right[i]) {
                return false;
            }
        }
        return true;
    }
}

struct RefUseDef {
    public bool[] usedRegs;
    public bool[] usedLocals;
    public bool[] definedRegs;
    public bool[] definedLocals;

    public RefUseDef(bool[] usedRegs, bool[] usedLocals, bool[] definedRegs, bool[] definedLocals) {
        this.usedLocals = usedLocals;
        this.usedRegs = usedRegs;
        this.definedLocals = definedLocals;
        this.definedRegs = definedRegs;
    }
}

struct BlockRefUseDef {
    public bool[] usedRegs;
    public bool[] usedLocals;
    public bool[] definedRegs;
    public bool[] definedLocals;

    public BlockRefUseDef(int regCount, int localCount) {
        usedRegs = new bool[regCount];
        definedRegs = new bool[regCount];
        usedLocals = new bool[localCount];
        definedLocals = new bool[localCount];
    }
}

struct BlockLiveness {
    public bool[] liveInRegs;
    public bool[] liveInLocals;
    public bool[] liveOutRegs;
    public bool[] liveOutLocals;

    public BlockLiveness(int regCount, int localCount) {
        liveInRegs = new bool[regCount];
        liveInLocals = new bool[localCount];
        liveOutRegs = new bool[regCount];
        liveOutLocals = new bool[localCount];
    }
}
