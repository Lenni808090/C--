using CMinus.Compiler.Lowering;
using CMinus.Runtime;
using Microsoft.VisualBasic;

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

    public CompiledProgram GenerateProgramm() {
        List<CompiledFunction> compiledFunctions = new();
        foreach (IrFunction irFunction in irCompiledUnit.irFunctions) {
            compiledFunctions.Add(GenerateFunction(irFunction));
        }

        return new CompiledProgram(compiledFunctions.ToArray(), irCompiledUnit.mainFunctionInd);
    }

    public CompiledFunction GenerateFunction(IrFunction irFunction) {

        foreach (BasicBlock basicBlock in irFunction.basicBlocks) {
            blockLabels.Add(basicBlock.blockId, functionBuilder.Emitter.NewLabel());
        }

        foreach (BasicBlock block in irFunction.basicBlocks) {
            EmitBlock(block);
        }

        int localCount = irFunction.localCount;
        blockLabels.Clear();

        return functionBuilder.BuildAndReset(localCount, irFunction.paramCount, irFunction.maxVReg);
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
            case IrCallInstr call: {
                    EmitCall(call);
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
        functionBuilder.RecordLocation(loadConst.location);
        int constInd = newConst(loadConst.valueType, loadConst.rawValue);
        functionBuilder.Emitter.EmitLoadConstant((ushort)loadConst.dstReg, (ushort)constInd);
    }

    void EmitStoreLocal(IrStoreLocal storeLocal) {
        functionBuilder.RecordLocation(storeLocal.location);
        functionBuilder.Emitter.EmitStoreLocal((ushort)storeLocal.srcReg, (ushort)storeLocal.localIndex);
    }

    void EmitLoadLocal(IrLoadLocal loadLocal) {
        functionBuilder.RecordLocation(loadLocal.location);
        functionBuilder.Emitter.EmitLoadLocal((ushort)loadLocal.dstReg, (ushort)loadLocal.localIndex);
    }

    void EmitBinaryOp(IrBinaryOp binaryOp) {
        functionBuilder.RecordLocation(binaryOp.location);
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
        functionBuilder.RecordLocation(unary.location);
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

    void EmitCall(IrCallInstr irCallInstr) {
        functionBuilder.RecordLocation(irCallInstr.location);

        ushort dst = (ushort)irCallInstr.dstReg;
        ushort[] argRegs = irCallInstr.argRegs.Select(a => (ushort)a).ToArray();
        ushort argCount = (ushort)irCallInstr.argCount;
        ushort functionIndex = (ushort)irCallInstr.functionIndex;

        functionBuilder.Emitter.EmitCall(dst, functionIndex, argCount, argRegs);
    }

    void EmitReturn(IrReturn @return) {
        functionBuilder.RecordLocation(@return.location);
        functionBuilder.Emitter.EmitReturn((ushort)@return.returnReg);
    }

    void EmitGoto(IrGoto @goto) {
        functionBuilder.RecordLocation(@goto.location);
        var gotoLabel = blockLabels[@goto.basicBlockId];
        functionBuilder.Emitter.EmitJump(gotoLabel);
    }
    void EmitBranch(IrBranch branch) {
        functionBuilder.RecordLocation(branch.location);
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
