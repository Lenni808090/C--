using CMinus.Compiler.Lowering;
using CMinus.Runtime;

namespace CMinus.CodeGen;

class CodeGenerator {
    FunctionBuilder functionBuilder;
    IrCompiledUnit irCompiledUnit;


    Dictionary<int, Label> blockLabels;
    public CodeGenerator(IrCompiledUnit irCompiledUnit) {
        blockLabels = new();
        functionBuilder = new();
        this.irCompiledUnit = irCompiledUnit;
    }

    public CompiledFunction GenerateFunction() {


        foreach (BasicBlock basicBlock in irCompiledUnit.basicBlocks) {
            blockLabels.Add(basicBlock.blockId, functionBuilder.Emitter.NewLabel());
        }

        foreach (BasicBlock block in irCompiledUnit.basicBlocks) {
            EmitBlock(block);
        }
        int localCount = irCompiledUnit.localCount;
        return functionBuilder.Build(localCount, irCompiledUnit.maxVReg);
    }


    void EmitBlock(BasicBlock basicBlock) {
        functionBuilder.Emitter.DefineLabel(blockLabels[basicBlock.blockId]);
        foreach (IrInstr instr in basicBlock.irInstrs) {
            EmitInstr(instr);
        }

        if (basicBlock.terminator is null) {
            throw new Exception($"BasicBlock {basicBlock.blockId} missing terminator");
        }

        EmitTerminator(basicBlock.terminator);
    }


    void EmitInstr(IrInstr instr) {
        switch (instr) {
            case IrLoadConst loadConst: {
                    EmitLoadConst(loadConst);
                    break;
                }
            case IrStoreLocal storeLocal: {
                    EmitStoreLocal(storeLocal);
                    break;
                }
            case IrLoadLocal loadLocal: {
                    EmitLoadLocal(loadLocal);
                    break;
                }
            case IrBinaryOp binaryOp: {
                    EmitBinaryOp(binaryOp);
                    break;
                }
            default: {
                    throw new Exception("Unkown instruction in codegen");
                }

        }
    }

    void EmitTerminator(Terminator terminator) {
        switch (terminator) {
            case IrReturn @return: {
                    EmitReturn(@return);
                    break;
                }
            case IrBranch branch: {
                    EmitBranch(branch);
                    break;
                }
            case IrGoto @goto: {
                    EmitGoto(@goto);
                    break;
                }
            default: {
                    throw new Exception("unkown terminator in codegen");
                }
        }
    }

    void EmitLoadConst(IrLoadConst loadConst) {
        int constInd = newConst(loadConst.valueType, loadConst.rawValue);
        functionBuilder.Emitter.EmitLoadConstant((byte)loadConst.dstReg, (byte)constInd);
    }

    void EmitStoreLocal(IrStoreLocal storeLocal) {
        functionBuilder.Emitter.EmitStoreLocal((byte)storeLocal.srcReg, (byte)storeLocal.localIndex);
    }

    void EmitLoadLocal(IrLoadLocal loadLocal) {
        functionBuilder.Emitter.EmitLoadLocal((byte)loadLocal.dstReg, (byte)loadLocal.localIndex);
    }

    void EmitBinaryOp(IrBinaryOp binaryOp) {
        byte dst = (byte)binaryOp.dstReg;
        byte left = (byte)binaryOp.leftReg;
        byte right = (byte)binaryOp.rightReg;

        switch (binaryOp.irBinaryOP) {
            case IrBinaryOPKind.AddInt: {
                    functionBuilder.Emitter.EmitAddInt(dst, left, right);
                    break;
                }
            case IrBinaryOPKind.SubtractInt: {
                    functionBuilder.Emitter.EmitSubtractInt(dst, left, right);
                    break;
                }
            case IrBinaryOPKind.MultiplyInt: {
                    functionBuilder.Emitter.EmitMultiplyInt(dst, left, right);
                    break;
                }
            case IrBinaryOPKind.DivideInt: {
                    functionBuilder.Emitter.EmitDivideInt(dst, left, right);
                    break;
                }

            case IrBinaryOPKind.CmpLtInt: {
                    functionBuilder.Emitter.EmitCmpLTInt(dst, left, right);
                    break;
                }
            case IrBinaryOPKind.CmpLtEInt: {
                    functionBuilder.Emitter.EmitCmpLTEInt(dst, left, right);
                    break;
                }
            case IrBinaryOPKind.CmpMtInt: {
                    functionBuilder.Emitter.EmitCmpMTInt(dst, left, right);
                    break;
                }
            case IrBinaryOPKind.CmpMtEInt: {
                    functionBuilder.Emitter.EmitCmpMTEInt(dst, left, right);
                    break;
                }
            case IrBinaryOPKind.CmpEq: {
                    functionBuilder.Emitter.EmitCmpEQ(dst, left, right);
                    break;
                }
            case IrBinaryOPKind.CmpNEq: {
                    functionBuilder.Emitter.EmitCmpNEQ(dst, left, right);
                    break;
                }

            default: {
                    throw new Exception($"Unknown IrBinaryOPKind: {binaryOp.irBinaryOP}");
                }
        }
    }

    void EmitReturn(IrReturn @return) {
        functionBuilder.Emitter.EmitReturn((byte)@return.returnReg);
    }

    void EmitGoto(IrGoto @goto) {
        var gotoLabel = blockLabels[@goto.basicBlockId];
        functionBuilder.Emitter.EmitJump(gotoLabel);
    }
    void EmitBranch(IrBranch branch) {
        var thenLabel = blockLabels[branch.thenBlockId];
        var elseLabel = blockLabels[branch.elseBlockId];

        functionBuilder.Emitter.EmitJumpIfFalse((byte)branch.condReg, elseLabel);
        functionBuilder.Emitter.EmitJump(thenLabel);
    }
    int newConst(Runtime.ValueType type, long value) {
        var _const = new Value(type, value);
        return functionBuilder.AddConstant(_const);
    }
}
