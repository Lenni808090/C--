using CMinus.Compiler.Lowering;
using CMinus.Runtime;

namespace CMinus.CodeGen;

class CodeGenerator {
    FunctionBuilder functionBuilder;
    IrCompiledUnit irCompiledUnit;
    public Value[] Constants => functionBuilder.Constants;


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

    public CompiledProgram GenerateProgram() {
        var entryFunction = GenerateFunction();
        return new CompiledProgram(new[] { entryFunction }, 0);
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
            case IrUnary unary: {
                    EmitUnary(unary);
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
        functionBuilder.Emitter.EmitLoadConstant((ushort)loadConst.dstReg, (ushort)constInd);
    }

    void EmitStoreLocal(IrStoreLocal storeLocal) {
        functionBuilder.Emitter.EmitStoreLocal((ushort)storeLocal.srcReg, (ushort)storeLocal.localIndex);
    }

    void EmitLoadLocal(IrLoadLocal loadLocal) {
        functionBuilder.Emitter.EmitLoadLocal((ushort)loadLocal.dstReg, (ushort)loadLocal.localIndex);
    }

    void EmitBinaryOp(IrBinaryOp binaryOp) {
        var dst = (ushort)binaryOp.dstReg;
        var left = (ushort)binaryOp.leftReg;
        var right = (ushort)binaryOp.rightReg;

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

    void EmitUnary(IrUnary unary) {
        var dst = (ushort)unary.dstReg;
        var opearand = (ushort)unary.operandReg;

        switch (unary.irUnaryOp) {
            case IrUnaryOpKind.NegInt: {
                    functionBuilder.Emitter.EmitNegInt(dst, opearand);
                    break;
                }
            case IrUnaryOpKind.Not: {
                    functionBuilder.Emitter.EmitNot(dst, opearand);
                    break;
                }
            default: {
                    throw new Exception($"Unknown IrUnaryOPKind: {unary.irUnaryOp}");
                }
        }
    }

    void EmitReturn(IrReturn @return) {
        functionBuilder.Emitter.EmitReturn((ushort)@return.returnReg);
    }

    void EmitGoto(IrGoto @goto) {
        var gotoLabel = blockLabels[@goto.basicBlockId];
        functionBuilder.Emitter.EmitJump(gotoLabel);
    }
    void EmitBranch(IrBranch branch) {
        var thenLabel = blockLabels[branch.thenBlockId];
        var elseLabel = blockLabels[branch.elseBlockId];

        functionBuilder.Emitter.EmitJumpIfFalse((ushort)branch.condReg, elseLabel);
        functionBuilder.Emitter.EmitJump(thenLabel);
    }
    int newConst(Runtime.ValueType type, long value) {
        var _const = new Value(type, value);
        return functionBuilder.AddConstant(_const);
    }
}
