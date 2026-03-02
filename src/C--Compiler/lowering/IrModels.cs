namespace CMinus.Compiler.Lowering;

abstract class IrInstr { };
abstract class Terminator { };

class IrCompiledUnit {
    public BasicBlock[] basicBlocks;

    public int localCount;
    public int maxVReg;

    public IrCompiledUnit(BasicBlock[] basicBlocks, int localCount, int maxVReg) {
        this.basicBlocks = basicBlocks;
        this.localCount = localCount;
        this.maxVReg = maxVReg;
    }
}

class BasicBlock {
    public List<IrInstr> irInstrs;

    public Terminator? terminator;
    public int blockId;

    public BasicBlock(int blockId) {
        irInstrs = new();
        this.blockId = blockId;
    }
}


class IrLoadConst : IrInstr {

    public Runtime.ValueType valueType;
    public long rawValue;
    public int dstReg;

    public IrLoadConst(Runtime.ValueType valueType, long rawValue, int dstReg) {
        this.valueType = valueType;
        this.rawValue = rawValue;
        this.dstReg = dstReg;
    }
}

class IrStoreLocal : IrInstr {
    public int srcReg;

    public int localIndex;

    public IrStoreLocal(int srcReg, int localIndex) {
        this.srcReg = srcReg;
        this.localIndex = localIndex;
    }
}

class IrLoadLocal : IrInstr {
    public int dstReg;
    public int localIndex;

    public IrLoadLocal(int dstReg, int localIndex) {
        this.dstReg = dstReg;
        this.localIndex = localIndex;
    }
}

class IrReturn : Terminator {
    public int returnReg;

    public IrReturn(int returnReg) {
        this.returnReg = returnReg;
    }
}

class IrMove : IrInstr {
    public int dstReg;
    public int srcReg;

    public IrMove(int dstReg, int srcReg) {
        this.dstReg = dstReg;
        this.srcReg = srcReg;
    }
}

class IrBinaryOp : IrInstr {
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


class IrGoto : Terminator {
    public int basicBlockId;

    public IrGoto(int basicBlockId) {
        this.basicBlockId = basicBlockId;
    }
}


class IrBranch : Terminator {
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

    // Int comparisons
    CmpEqInt,
    CmpNEqInt,
    CmpLtInt,
    CmpLtEInt,
    CmpMtInt,
    CmpMtEInt,

    // Bool comparisons
    CmpEqBool,
    CmpNEqBool,
}