namespace CMinus.Compiler.Lowering;

using CMinus.Compiler;

abstract class IrInstr;
abstract class Terminator;

sealed class IrCompiledUnit {
    public IrFunction[] irFunctions;
    public int mainFunctionInd;
    public CMinus.CodeGen.RuntimeTypeDesc[] typeTable;
    public CMinus.CodeGen.Value[] constants;

    public IrCompiledUnit(IrFunction[] irFunctions, int mainFunctionInd, CMinus.CodeGen.RuntimeTypeDesc[] typeTable, CMinus.CodeGen.Value[] constants) {
        this.irFunctions = irFunctions;
        this.mainFunctionInd = mainFunctionInd;
        this.typeTable = typeTable;
        this.constants = constants;
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
    public int constIndex;
    public int dstReg;

    public IrLoadConst(int constIndex, int dstReg) {
        this.constIndex = constIndex;
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

sealed class IrNewArray : IrInstr {
    public int dstReg;
    public int typeId;

    public int lengthReg;

    public IrNewArray(int dstReg, int typeId, int lengthReg) {
        this.dstReg = dstReg;
        this.typeId = typeId;
        this.lengthReg = lengthReg;
    }
}

sealed class IrArrayLength : IrInstr {
    public int dstReg;
    public int arrayReg;

    public IrArrayLength(int dstReg, int arrayReg) {
        this.dstReg = dstReg;
        this.arrayReg = arrayReg;
    }

}

sealed class IrStoreElement : IrInstr {
    public int srcReg;
    public int arrayReg;
    public int indexReg;

    public IrStoreElement(int srcReg, int arrayReg, int indexReg) {
        this.srcReg = srcReg;
        this.arrayReg = arrayReg;
        this.indexReg = indexReg;
    }
}

sealed class IrLoadElement : IrInstr {
    public int dstReg;
    public int arrayReg;
    public int indexReg;

    public IrLoadElement(int dstReg, int arrayReg, int indexReg) {
        this.dstReg = dstReg;
        this.arrayReg = arrayReg;
        this.indexReg = indexReg;
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


sealed class IrCallInstr : IrInstr {
    public int dstReg;
    public int argCount;

    public int functionIndex;
    public int[] argRegs;

    public IrCallInstr(int dstReg, int argCount, int[] argRegs, int functionIndex) {
        this.dstReg = dstReg;
        this.functionIndex = functionIndex;
        this.argCount = argCount;
        this.argRegs = argRegs;
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
    ModulusInt,
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
