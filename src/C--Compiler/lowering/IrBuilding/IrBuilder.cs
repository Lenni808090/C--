using CMinus.Compiler.Binding;

namespace CMinus.Compiler.Lowering;

class IrBuilder {
    List<BasicBlock> basicBlocks;
    Stack<LoopTarget> loopTarget;
    BasicBlock currentBlock;

    BoundCompiledUnit boundCompiledUnit;
    int nextBlockId;
    int nextTempLocalInd;
    int nextVReg;
    int maxVReg;

    public IrBuilder(BoundCompiledUnit boundCompiledUnit) {
        basicBlocks = new();
        loopTarget = new();
        this.boundCompiledUnit = boundCompiledUnit;
        nextTempLocalInd = boundCompiledUnit.localCount;

        currentBlock = MakeNewBlock();
        basicBlocks.Add(currentBlock);
    }

    public IrCompiledUnit BuildCompiledUnit() {
        var stmts = boundCompiledUnit.boundStmts;

        foreach (var stmt in stmts) {
            if (currentBlock.terminator is not null) {
                var unreachable = CreateUnreachableBlock();
                SwitchCurrentBlock(unreachable);
            }
            BuildStmt(stmt);
        }

        return new IrCompiledUnit(basicBlocks.ToArray(), boundCompiledUnit.localCount, maxVReg);
    }

    void BuildStmt(BoundStmt boundStmt) {
        switch (boundStmt) {
            case BoundVarDeclarationStmt v: {
                    BuildVarDeclarationStmt(v);
                    break;
                }
            case BoundContinueStmt c: {
                    BuildContinueStmt(c);
                    break;
                }
            case BoundBreakStmt b: {
                    BuildBrealStmt(b);
                    break;
                }
            case BoundReturnStmt r: {
                    BuildReturnStmt(r);
                    break;
                }
            case BoundIfStmt i: {
                    BuildIfStmt(i);
                    break;
                }
            case BoundWhileStmt w: {
                    BuildWhileStmt(w);
                    break;
                }
            case BoundExpressionStmt e: {
                    BuildExpressionStmt(e);
                    break;
                }
            case BoundVarAssignmentStmt va: {
                    BuildVarAssignmentStmt(va);
                    break;
                }
            case BoundBlockStmt b: {
                    BuildBlockStmt(b);
                    break;
                }
            default: {
                    throw new Exception("Unkown Stmt in Build Stmt Ir" + boundStmt);
                }
        }
    }

    void BuildVarDeclarationStmt(BoundVarDeclarationStmt v) {
        int localIndex = v.localSymbol.index;
        int srcReg = BuildExpr(v.initializer);
        EmitStoreLocal(srcReg, localIndex);
    }

    void BuildContinueStmt(BoundContinueStmt continueStmt) {
        var condBlockId = loopTarget.Peek().condBlockId;
        TerminateGoto(condBlockId);
    }

    void BuildBrealStmt(BoundBreakStmt breakStmt) {
        var endBlockId = loopTarget.Peek().endBlockId;
        TerminateGoto(endBlockId);
    }

    void BuildReturnStmt(BoundReturnStmt returnStmt) {
        int returnedReg = BuildExpr(returnStmt.boundReturnedExpr);
        TerminateReturn(returnedReg);
    }

    void BuildIfStmt(BoundIfStmt ifStmt) {
        int conditionResReg = BuildExpr(ifStmt.boundConditionExpr);

        var thenBlock = CreateBlock();
        var elseBlock = ifStmt.elseStmt is null ? null : CreateBlock();
        var mergeBlock = CreateBlock();

        TerminateBranch(conditionResReg, thenBlock.blockId, elseBlock is null ? mergeBlock.blockId : elseBlock.blockId);

        SwitchCurrentBlock(thenBlock);
        BuildStmt(ifStmt.thenStmt);
        if (currentBlock.terminator is null) {
            TerminateGoto(mergeBlock.blockId);
        }

        if (elseBlock is not null) {
            SwitchCurrentBlock(elseBlock);
            BuildStmt(ifStmt.elseStmt!);
            if (currentBlock.terminator is null) {
                TerminateGoto(mergeBlock.blockId);
            }
        }

        SwitchCurrentBlock(mergeBlock);
    }


    void BuildWhileStmt(BoundWhileStmt whileStmt) {
        var condBlock = CreateBlock();
        var whileBlock = CreateBlock();
        var endBlock = CreateBlock();

        loopTarget.Push(CreateLoopTarget(condBlock, endBlock));

        if (currentBlock.terminator is null) {
            TerminateGoto(condBlock.blockId);
        }

        SwitchCurrentBlock(condBlock);
        int resReg = BuildExpr(whileStmt.boundConditionExpr);
        TerminateBranch(resReg, whileBlock.blockId, endBlock.blockId);

        SwitchCurrentBlock(whileBlock);
        BuildStmt(whileStmt.body);

        if (currentBlock.terminator is null) {
            TerminateGoto(condBlock.blockId);
        }

        SwitchCurrentBlock(endBlock);
        loopTarget.Pop();
    }

    void BuildExpressionStmt(BoundExpressionStmt expressionStmt) {
        BuildExpr(expressionStmt.boundExpr);
    }


    void BuildVarAssignmentStmt(BoundVarAssignmentStmt assignmentStmt) {
        int dstReg = BuildExpr(assignmentStmt.assignmentExpr);
        var localIndex = assignmentStmt.localSymbol.index;
        EmitStoreLocal(dstReg, localIndex);
    }

    void BuildBlockStmt(BoundBlockStmt blockStmt) {
        foreach (var stmt in blockStmt.boundStmts) {
            if (currentBlock.terminator is not null) {
                var unreachable = CreateUnreachableBlock();
                SwitchCurrentBlock(unreachable);
            }
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
                    throw new Exception("Unkown Expr in Build Expr Ir" + boundExpr);
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
        else if (opKind == BoundBinaryOperatorKind.LogicalAnd) {
            return BuildLogicalAndExpr(binaryExpr);
        }

        int leftReg = BuildExpr(binaryExpr.leftBoundExpr);
        int rightReg = BuildExpr(binaryExpr.rightBoundExpr);

        var irOpKind = MapBinaryOp(opKind);

        return EmitBinary(irOpKind, leftReg, rightReg);
    }

    int BuildLogicalOrExpr(BoundBinaryExpr binaryExpr) {
        int tempIndex = AllocTempLocalInd();

        int leftReg = BuildExpr(binaryExpr.leftBoundExpr);

        var rhsBlock = CreateBlock();
        var leftTrueBlock = CreateBlock();
        var mergeBlock = CreateBlock();

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

    int BuildLogicalAndExpr(BoundBinaryExpr binaryExpr) {
        int tempIndex = AllocTempLocalInd();

        int leftReg = BuildExpr(binaryExpr.leftBoundExpr);

        var falsyBranch = CreateBlock();
        var rhsBlock = CreateBlock();
        var mergeBlock = CreateBlock();

        TerminateBranch(leftReg, rhsBlock.blockId, falsyBranch.blockId);

        SwitchCurrentBlock(falsyBranch);
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
        if (currentBlock.terminator is not null) {
            throw new Exception("there is already a terminator in this block");
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


    BasicBlock CreateBlock() {
        var newBlock = MakeNewBlock();
        basicBlocks.Add(newBlock);
        return newBlock;
    }

    BasicBlock CreateUnreachableBlock() {
        var newBlock = new BasicBlock(GetBlockId(), true);
        basicBlocks.Add(newBlock);
        return newBlock;
    }

    void SwitchCurrentBlock(BasicBlock basicBlock) {
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
    int EmitBinary(IrBinaryOPKind op, int leftReg, int rightReg) {
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

    LoopTarget CreateLoopTarget(BasicBlock condBlock, BasicBlock endBlock) {
        return new LoopTarget(condBlock.blockId, endBlock.blockId);
    }


    Runtime.ValueType GetValueType(SymbolType symbolType) {
        return symbolType switch {
            SymbolType.Int => Runtime.ValueType.Int,
            SymbolType.Bool => Runtime.ValueType.Bool,
            _ => throw new Exception("Unkown symbol type in get value type" + symbolType),
        };
    }


    IrBinaryOPKind MapBinaryOp(BoundBinaryOperatorKind kind) {
        return kind switch {
            BoundBinaryOperatorKind.AddInt => IrBinaryOPKind.AddInt,
            BoundBinaryOperatorKind.SubtractInt => IrBinaryOPKind.SubtractInt,
            BoundBinaryOperatorKind.MultiplyInt => IrBinaryOPKind.MultiplyInt,
            BoundBinaryOperatorKind.DivideInt => IrBinaryOPKind.DivideInt,

            BoundBinaryOperatorKind.Equals => IrBinaryOPKind.CmpEq,
            BoundBinaryOperatorKind.NotEquals => IrBinaryOPKind.CmpNEq,
            BoundBinaryOperatorKind.LessThanInt => IrBinaryOPKind.CmpLtInt,
            BoundBinaryOperatorKind.LessThanOrEqualInt => IrBinaryOPKind.CmpLtEInt,
            BoundBinaryOperatorKind.GreaterThanInt => IrBinaryOPKind.CmpMtInt,
            BoundBinaryOperatorKind.GreaterThanOrEqualInt => IrBinaryOPKind.CmpMtEInt,


            _ => throw new Exception("Unsupported binary operator: " + kind),
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
