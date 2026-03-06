namespace CMinus.Compiler.Lowering;

abstract class IrInstr { };
abstract class Terminator { };

sealed class IrCompiledUnit {
    public BasicBlock[] basicBlocks;

    public int localCount;
    public int maxVReg;

    public IrCompiledUnit(BasicBlock[] basicBlocks, int localCount, int maxVReg) {
        this.basicBlocks = basicBlocks;
        this.localCount = localCount;
        this.maxVReg = maxVReg;
    }
}

sealed class BasicBlock {
    public List<IrInstr> irInstrs;
    public bool isUnreachable;
    public Terminator? terminator;
    public int blockId;

    public BasicBlock(int blockId, bool isUnreachable = false) {
        irInstrs = new();
        this.blockId = blockId;
        this.isUnreachable = isUnreachable;
    }
}


sealed class IrLoadConst : IrInstr {

    public Runtime.ValueType valueType;
    public long rawValue;
    public int dstReg;

    public IrLoadConst(Runtime.ValueType valueType, long rawValue, int dstReg) {
        this.valueType = valueType;
        this.rawValue = rawValue;
        this.dstReg = dstReg;
    }
}

sealed class IrStoreLocal : IrInstr {
    public int srcReg;

    public int localIndex;

    public IrStoreLocal(int srcReg, int localIndex) {
        this.srcReg = srcReg;
        this.localIndex = localIndex;
    }
}

sealed class IrLoadLocal : IrInstr {
    public int dstReg;
    public int localIndex;

    public IrLoadLocal(int dstReg, int localIndex) {
        this.dstReg = dstReg;
        this.localIndex = localIndex;
    }
}

sealed class IrReturn : Terminator {
    public int returnReg;

    public IrReturn(int returnReg) {
        this.returnReg = returnReg;
    }
}

sealed class IrMove : IrInstr {
    public int dstReg;
    public int srcReg;

    public IrMove(int dstReg, int srcReg) {
        this.dstReg = dstReg;
        this.srcReg = srcReg;
    }
}

sealed class IrBinaryOp : IrInstr {
    public IrBinaryOPKind irBinaryOP;

    public int dstReg;
    public int leftReg;
    public int rightReg;

    public IrBinaryOp(IrBinaryOPKind irBinaryOP, int dstReg, int leftReg, int rightReg) {
        this.irBinaryOP = irBinaryOP;
        this.dstReg = dstReg;
        this.leftReg = leftReg;
        this.rightReg = rightReg;
    }
}

sealed class IrUnary : IrInstr {
    public int dstReg;
    public int operandReg;

    public IrUnaryOpKind irUnaryOp;

    public IrUnary(int dstReg, int operandReg, IrUnaryOpKind irUnaryOp) {
        this.dstReg = dstReg;
        this.operandReg = operandReg;
        this.irUnaryOp = irUnaryOp;
    }
}


sealed class IrGoto : Terminator {
    public int basicBlockId;

    public IrGoto(int basicBlockId) {
        this.basicBlockId = basicBlockId;
    }
}


sealed class IrBranch : Terminator {
    public int condReg;
    public int thenBlockId;

    public int elseBlockId;

    public IrBranch(int condReg, int thenBlockId, int elseBlockId) {
        this.condReg = condReg;
        this.thenBlockId = thenBlockId;
        this.elseBlockId = elseBlockId;
    }
}



enum IrBinaryOPKind {
    // Arithmetic (int)
    AddInt,
    SubtractInt,
    MultiplyInt,
    DivideInt,

    CmpEq,
    CmpNEq,

    CmpLtInt,
    CmpLtEInt,
    CmpMtInt,
    CmpMtEInt,

}

enum IrUnaryOpKind {
    NegInt,
    Not,
}


sealed class LoopTarget {
    public int continueBlockId;
    public int endBlockId;

    public LoopTarget(int continueBlockId, int endBlockId) {
        this.continueBlockId = continueBlockId;
        this.endBlockId = endBlockId;
    }
}
