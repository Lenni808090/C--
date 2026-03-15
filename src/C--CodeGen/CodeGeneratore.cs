using CMinus.Compiler.Lowering;

namespace CMinus.CodeGen;

class CodeGenerator {
    FunctionBuilder functionBuilder;
    IrCompiledUnit irCompiledUnit;
    StackBuilder stackBuilder;
    Dictionary<int, Label> blockLabels;
    public CodeGenerator(IrCompiledUnit irCompiledUnit, FunctionStackMap[] functionStacks) {
        blockLabels = new();
        stackBuilder = new(functionStacks);
        functionBuilder = new();
        this.irCompiledUnit = irCompiledUnit;
    }

    public CompiledProgram GenerateProgramm() {
        List<CompiledFunction> compiledFunctions = new();
        for (int i = 0; i < irCompiledUnit.irFunctions.Length; i++) {
            var irFunction = irCompiledUnit.irFunctions[i];
            compiledFunctions.Add(GenerateFunction(irFunction, i));
        }
        var stackMap = stackBuilder.GetByteoffsetStackMap();
        return new CompiledProgram(compiledFunctions.ToArray(), irCompiledUnit.constants, irCompiledUnit.mainFunctionInd, irCompiledUnit.typeTable, irCompiledUnit.nativeFunctionNames, stackMap);
    }

    public CompiledFunction GenerateFunction(IrFunction irFunction, int functionIndex) {
        StartFunction(functionIndex);

        foreach (BasicBlock basicBlock in irFunction.basicBlocks) {
            blockLabels.Add(basicBlock.blockId, functionBuilder.Emitter.NewLabel());
        }

        foreach (BasicBlock block in irFunction.basicBlocks) {
            EmitBlock(block);
        }

        stackBuilder.BuildFunctionByteoffsetStackMap();
        int localCount = irFunction.localCount;
        return functionBuilder.BuildAndReset(localCount, irFunction.paramCount, irFunction.maxVReg);
    }

    void EmitBlock(BasicBlock basicBlock) {
        functionBuilder.Emitter.DefineLabel(blockLabels[basicBlock.blockId]);

        for (int i = 0; i < basicBlock.irInstrs.Count(); i++) {
            var instr = basicBlock.irInstrs[i];
            EmitInstr(instr, basicBlock.blockId, i);
        }

        if (basicBlock.terminator is null) {
            throw new Exception($"BasicBlock {basicBlock.blockId} missing terminator");
        }

        EmitTerminator(basicBlock.terminator);
    }


    void EmitInstr(IrInstr instr, int blockId, int instrIndex) {
        stackBuilder.TryRecordByteoffsetStackMap(blockId, instrIndex, functionBuilder.Emitter.pos);

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
            case IrCall call: {
                    EmitCall(call);
                    break;
                }
            case IrNewArray newArray: {
                    EmitNewArray(newArray);
                    break;
                }
            case IrStoreElement storeElement: {
                    EmitStoreElement(storeElement);
                    break;
                }
            case IrLoadElement loadElement: {
                    EmitLoadElement(loadElement);
                    break;
                }
            case IrArrayLength arrayLength: {
                    EmitArrayLength(arrayLength);
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
        functionBuilder.Emitter.EmitLoadConstant((ushort)loadConst.dstReg, (ushort)loadConst.constIndex);
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
            case IrBinaryOPKind.ModulusInt: {
                    functionBuilder.Emitter.EmitModulusInt(dst, left, right);
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

    void EmitCall(IrCall irCallInstr) {
        ushort dst = (ushort)irCallInstr.dstReg;
        ushort[] argRegs = irCallInstr.argRegs.Select(a => (ushort)a).ToArray();
        ushort argCount = (ushort)irCallInstr.argCount;
        ushort functionIndex = (ushort)irCallInstr.functionIndex;

        functionBuilder.Emitter.EmitCall(dst, functionIndex, argCount, argRegs);
    }

    void EmitNewArray(IrNewArray newArray) {
        functionBuilder.Emitter.EmitNewArray((ushort)newArray.dstReg, newArray.typeId, (ushort)newArray.lengthReg);
    }

    void EmitStoreElement(IrStoreElement storeElement) {
        functionBuilder.Emitter.EmitStoreElement((ushort)storeElement.srcReg, (ushort)storeElement.arrayReg, (ushort)storeElement.indexReg);
    }

    void EmitLoadElement(IrLoadElement loadElement) {
        functionBuilder.Emitter.EmitLoadElement((ushort)loadElement.dstReg, (ushort)loadElement.arrayReg, (ushort)loadElement.indexReg);
    }

    void EmitArrayLength(IrArrayLength arrayLength) {
        functionBuilder.Emitter.EmitArrayLength((ushort)arrayLength.dstReg, (ushort)arrayLength.arrayReg);
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

    void StartFunction(int functionIndex) {
        blockLabels.Clear();
        stackBuilder.Reset();
        stackBuilder.FillPosToStack(functionIndex);
    }

}
