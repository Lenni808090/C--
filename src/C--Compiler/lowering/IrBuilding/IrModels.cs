namespace CMinus.Compiler.Lowering;

using CMinus.Compiler;

abstract class IrInstr {
    public SourceLocation location {
        get;
    }

    protected IrInstr(SourceLocation location) {
        this.location = location;
    }
};
abstract class Terminator {
    public SourceLocation location {
        get;
    }

    protected Terminator(SourceLocation location) {
        this.location = location;
    }
};

sealed class IrCompiledUnit {
    public IrFunction[] irFunctions;
    public int mainFunctionInd;
    public IrCompiledUnit(IrFunction[] irFunctions, int mainFunctionInd) {
        this.irFunctions = irFunctions;
        this.mainFunctionInd = mainFunctionInd;
    }
}

sealed class IrFunction {
    public BasicBlock[] basicBlocks;
    public int localCount;

    public int paramCount;
    public int maxVReg;

    public IrFunction(BasicBlock[] basicBlocks, int localCount, int maxVReg, int paramCount) {
        this.basicBlocks = basicBlocks;
        this.localCount = localCount;
        this.maxVReg = maxVReg;
        this.paramCount = paramCount;
    }
}

sealed class BasicBlock {
    public List<IrInstr> irInstrs;
    public bool isUnreachable;
    public Terminator? terminator;
    public int blockId;
    public SourceLocation location;

    public BasicBlock(int blockId, bool isUnreachable = false) {
        irInstrs = new();
        this.blockId = blockId;
        this.isUnreachable = isUnreachable;
        location = SourceLocation.None;
    }
}


sealed class IrLoadConst : IrInstr {

    public Runtime.ValueType valueType;
    public long rawValue;
    public int dstReg;

    public IrLoadConst(Runtime.ValueType valueType, long rawValue, int dstReg, SourceLocation location) : base(location) {
        this.valueType = valueType;
        this.rawValue = rawValue;
        this.dstReg = dstReg;
    }
}

sealed class IrStoreLocal : IrInstr {
    public int srcReg;

    public int localIndex;

    public IrStoreLocal(int srcReg, int localIndex, SourceLocation location) : base(location) {
        this.srcReg = srcReg;
        this.localIndex = localIndex;
    }
}

sealed class IrLoadLocal : IrInstr {
    public int dstReg;
    public int localIndex;

    public IrLoadLocal(int dstReg, int localIndex, SourceLocation location) : base(location) {
        this.dstReg = dstReg;
        this.localIndex = localIndex;
    }
}

sealed class IrReturn : Terminator {
    public int returnReg;

    public IrReturn(int returnReg, SourceLocation location) : base(location) {
        this.returnReg = returnReg;
    }
}

sealed class IrMove : IrInstr {
    public int dstReg;
    public int srcReg;

    public IrMove(int dstReg, int srcReg, SourceLocation location) : base(location) {
        this.dstReg = dstReg;
        this.srcReg = srcReg;
    }
}

sealed class IrBinaryOp : IrInstr {
    public IrBinaryOPKind irBinaryOP;

    public int dstReg;
    public int leftReg;
    public int rightReg;

    public IrBinaryOp(IrBinaryOPKind irBinaryOP, int dstReg, int leftReg, int rightReg, SourceLocation location) : base(location) {
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

    public IrUnary(int dstReg, int operandReg, IrUnaryOpKind irUnaryOp, SourceLocation location) : base(location) {
        this.dstReg = dstReg;
        this.operandReg = operandReg;
        this.irUnaryOp = irUnaryOp;
    }
}

sealed class IrCallInstr : IrInstr {
    public int dstReg;
    public int argCount;

    public int functionIndex;
    public int[] argRegs;

    public IrCallInstr(int dstReg, int argCount, int[] argRegs, int functionIndex, SourceLocation location) : base(location) {
        this.dstReg = dstReg;
        this.functionIndex = functionIndex;
        this.argCount = argCount;
        this.argRegs = argRegs;
    }

}


sealed class IrGoto : Terminator {
    public int basicBlockId;

    public IrGoto(int basicBlockId, SourceLocation location) : base(location) {
        this.basicBlockId = basicBlockId;
    }
}


sealed class IrBranch : Terminator {
    public int condReg;
    public int thenBlockId;

    public int elseBlockId;

    public IrBranch(int condReg, int thenBlockId, int elseBlockId, SourceLocation location) : base(location) {
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
