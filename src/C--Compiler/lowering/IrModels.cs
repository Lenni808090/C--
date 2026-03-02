namespace CMinus.Compiler.Lowering;

abstract class IrInstr { };
abstract class Terminator { };

class IrCompiledUnit {
    public BasicBlock[] basicBlocks;

    int localCount;
    int maxVReg;

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
    public int constIndex;
    public int dstReg;

    public IrLoadConst(int constIndex, int dstReg) {
        this.constIndex = constIndex;
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


class IrBinaryOp : IrInstr {
    public IrBinaryOP irBinaryOP;

    public int dstReg;
    public int leftReg;
    public int rightReg;

    public IrBinaryOp(IrBinaryOP irBinaryOP, int dstReg, int leftReg, int rightReg) {
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



enum IrBinaryOP {
    AddInt,
    SubtractInt,
    DivideInt,
    MultiplyInt,

    CmpEqInt,
    CmpNEqInt,
    CmpLtInt,
    CmpLtEInt,
    CmpMtInt,
    CmpMtEInt,

}