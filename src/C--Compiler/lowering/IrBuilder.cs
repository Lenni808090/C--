using CMinus.Compiler.Binding;
using CMinus.Compiler.Syntax;

namespace CMinus.Compiler.Lowering;

class IrBuilder {
    List<BasicBlock> basicBlocks;
    BasicBlock currentBlock;

    BoundCompiledUnit boundCompiledUnit;
    int nextBlockId;
    int nextTempLocalInd;
    int nextVReg;
    int maxVReg;

    public IrBuilder(BoundCompiledUnit boundCompiledUnit) {
        basicBlocks = new();
        this.boundCompiledUnit = boundCompiledUnit;
        nextTempLocalInd = boundCompiledUnit.localCount;

        currentBlock = MakeNewBlock();
        basicBlocks.Add(currentBlock);
    }

    public IrCompiledUnit BuildCompiledUnit() {
        var stmts = boundCompiledUnit.boundStmts;

        foreach (var stmt in stmts) {
            BuildStmt(stmt);
        }

        if (currentBlock.terminator is null) {
            throw new Exception("block needs terminator");
        }

        return new IrCompiledUnit(basicBlocks.ToArray(), boundCompiledUnit.localCount, maxVReg);
    }

    void BuildStmt(BoundStmt boundStmt) {
        switch (boundStmt) {
            case BoundVarDeclarationStmt v: {
                    BuildVarDeclarationStmt(v);
                    break;
                }
            case BoundReturnStmt r: {
                    BuildReturnStmt(r);
                    break;
                }
            case BoundBlockStmt b: {
                    BuildBlockStmt(b);
                    break;
                }
            default: {
                    throw new Exception("Unkown Stmt in Build Stmt Ir");
                }
        }
    }

    void BuildVarDeclarationStmt(BoundVarDeclarationStmt v) {
        int localIndex = v.localSymbol.index;
        int srcReg = BuildExpr(v.initializer);
        EmitStoreLocal(srcReg, localIndex);
    }

    void BuildReturnStmt(BoundReturnStmt returnStmt) {
        int returnedReg = BuildExpr(returnStmt.boundReturnedExpr);
        TerminateReturn(returnedReg);
    }

    void BuildBlockStmt(BoundBlockStmt blockStmt) {
        foreach (var stmt in blockStmt.boundStmts) {
            BuildStmt(stmt);
        }
    }

    int BuildExpr(BoundExpr boundExpr) {
        switch (boundExpr) {
            case BoundLiteralExpr l: {
                    return BuildLiteralExpr(l);
                }
            case BoundNameExpr n: {
                    return BuildNameExpr(n);
                }
            case BoundBinaryExpr b: {
                    return BuildBinaryExpr(b);
                }
            default: {
                    throw new Exception("Unkown Expr in Build Expr Ir");
                }
        }
    }

    int BuildLiteralExpr(BoundLiteralExpr literalExpr) {
        var type = GetValueType(literalExpr.type);
        long value = literalExpr.value;
        return EmitLoadConst(type, value);
    }

    int BuildNameExpr(BoundNameExpr nameExpr) {
        int localIndex = nameExpr.localSymbol.index;
        return EmitLoadLocal(localIndex);
    }

    int BuildBinaryExpr(BoundBinaryExpr binaryExpr) {
        var opKind = binaryExpr.boundBinaryOperator.operatorKind;

        if (opKind == BoundBinaryOperatorKind.LogicalOr) {
            return BuildLogicalOrExpr(binaryExpr);
        }

        throw new Exception("Unhandled binary operator in IrBuilder: " + opKind);
    }

    int BuildLogicalOrExpr(BoundBinaryExpr binaryExpr) {
        int tempIndex = AllocTempLocalInd();

        int leftReg = BuildExpr(binaryExpr.leftBoundExpr);

        var rhsBlock = MakeNewBlock();
        var leftTrueBlock = MakeNewBlock();
        var mergeBlock = MakeNewBlock();

        TerminateBranch(leftReg, leftTrueBlock.blockId, rhsBlock.blockId);

        SwitchCurrentBlock(leftTrueBlock);
        EmitStoreLocal(leftReg, tempIndex);
        TerminateGoto(mergeBlock.blockId);

        SwitchCurrentBlock(rhsBlock);
        int rightReg = BuildExpr(binaryExpr.rightBoundExpr);
        EmitStoreLocal(rightReg, tempIndex);
        TerminateGoto(mergeBlock.blockId);

        SwitchCurrentBlock(mergeBlock);

        int resReg = EmitLoadLocal(tempIndex);

        return resReg;
    }

    void Emit(IrInstr irInstr) {
        if (currentBlock.terminator is null) {
            throw new Exception("Block already closed");
        }
        currentBlock.irInstrs.Add(irInstr);
    }

    void Terminate(Terminator terminator) {
        if (currentBlock.terminator is not null) {
            throw new Exception("onyl one terminator allowed");
        }
        currentBlock.terminator = terminator;
    }

    int GetBlockId() {
        return nextBlockId++;
    }

    BasicBlock MakeNewBlock() {
        return new BasicBlock(GetBlockId());
    }

    void CreateBlock() {
        var newBlock = MakeNewBlock();
        basicBlocks.Add(newBlock);
        currentBlock = newBlock;
    }

    void SwitchCurrentBlock(BasicBlock basicBlock) {
        basicBlocks.Add(basicBlock);
        currentBlock = basicBlock;
    }

    int AllocTempLocalInd() {
        return nextTempLocalInd++;
    }

    int EmitLoadConst(Runtime.ValueType type, long value) {
        int dstReg = AllocVReg();
        Emit(new IrLoadConst(type, value, dstReg));
        return dstReg;
    }

    void EmitStoreLocal(int srcReg, int localIndex) {
        Emit(new IrStoreLocal(srcReg, localIndex));
    }

    int EmitLoadLocal(int localIndex) {
        int dstReg = AllocVReg();
        Emit(new IrLoadLocal(dstReg, localIndex));
        return dstReg;
    }

    int EmitMove(int dstReg, int srcReg) {
        Emit(new IrMove(dstReg, srcReg));
        return dstReg;
    }
    int EmitBinary(IrBinaryOP op, int leftReg, int rightReg) {
        int dstReg = AllocVReg();
        Emit(new IrBinaryOp(op, dstReg, leftReg, rightReg));
        return dstReg;
    }

    void TerminateReturn(int returnReg) {
        Terminate(new IrReturn(returnReg));
    }


    void TerminateGoto(int targetBlockId) {
        Terminate(new IrGoto(targetBlockId));
    }


    void TerminateBranch(int condReg, int thenBlockId, int elseBlockId) {
        Terminate(new IrBranch(condReg, thenBlockId, elseBlockId));
    }



    Runtime.ValueType GetValueType(SymbolType symbolType) {
        return symbolType switch {
            SymbolType.Int => Runtime.ValueType.Int,
            SymbolType.Bool => Runtime.ValueType.Bool,
            _ => throw new Exception("Unkown symbol type in get value type" + symbolType),
        };
    }

    int AllocVReg() {
        int reg = nextVReg++;
        if (nextVReg > maxVReg) {
            maxVReg = nextVReg;
        }
        return reg;
    }
}