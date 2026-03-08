using System.Data;
using System.Runtime.CompilerServices;
using CMinus.Compiler.Binding;

namespace CMinus.Compiler.Lowering;

class IrBuilder {
    List<BasicBlock> basicBlocks;
    Stack<LoopTarget> loopTarget;

    Dictionary<FunctionSymbol, int> symbolToInd;
    BasicBlock currentBlock;

    BoundCompiledUnit boundCompiledUnit;
    int nextBlockId;
    int nextTempLocalInd;
    int nextVReg;
    int maxVReg;

    public IrBuilder(BoundCompiledUnit boundCompiledUnit) {
        symbolToInd = new();
        basicBlocks = new();
        loopTarget = new();
        this.boundCompiledUnit = boundCompiledUnit;
        currentBlock = MakeNewBlock();
    }

    public IrCompiledUnit BuildCompiledUnit() {
        var functionsToBuild = boundCompiledUnit.functions;
        List<IrFunction> builtFunctions = new();

        int mainFunctionInd = -1;

        fillSymbolToInd(functionsToBuild);

        for (int i = 0; i < functionsToBuild.Length; i++) {
            var function = functionsToBuild[i];

            if (ReferenceEquals(function, boundCompiledUnit.mainFunction)) {
                mainFunctionInd = i;
            }

            builtFunctions.Add(BuildFunction(function));
        }

        return new IrCompiledUnit(builtFunctions.ToArray(), mainFunctionInd);
    }

    public void fillSymbolToInd(BoundFunctionDeclaration[] functionDeclarations) {
        for (int i = 0; i < functionDeclarations.Length; i++) {
            var function = functionDeclarations[i];
            symbolToInd[function.functionSymbol] = i;
        }
    }

    public IrFunction BuildFunction(BoundFunctionDeclaration functionDeclaration) {
        StartFunction(functionDeclaration);

        var boundStmt = functionDeclaration.functionBody;
        BuildStmt(boundStmt);
        var irFunc = new IrFunction(basicBlocks.ToArray(), nextTempLocalInd, maxVReg, functionDeclaration.functionSymbol.argCount);

        return irFunc;
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
            case BoundForStmt f: {
                    BuildForStmt(f);
                    break;
                }
            case BoundExpressionStmt e: {
                    BuildExpressionStmt(e);
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
        EmitStoreLocal(srcReg, localIndex, v.location);
    }

    void BuildContinueStmt(BoundContinueStmt continueStmt) {
        var condBlockId = loopTarget.Peek().continueBlockId;
        TerminateGoto(condBlockId, continueStmt.location);
    }

    void BuildBrealStmt(BoundBreakStmt breakStmt) {
        var endBlockId = loopTarget.Peek().endBlockId;
        TerminateGoto(endBlockId, breakStmt.location);
    }

    void BuildReturnStmt(BoundReturnStmt returnStmt) {
        int returnedReg = BuildExpr(returnStmt.boundReturnedExpr);
        TerminateReturn(returnedReg, returnStmt.location);
    }

    void BuildIfStmt(BoundIfStmt ifStmt) {
        int conditionResReg = BuildExpr(ifStmt.boundConditionExpr);

        var thenBlock = CreateBlock();
        var elseBlock = ifStmt.elseStmt is null ? null : CreateBlock();
        var mergeBlock = CreateBlock();

        TerminateBranch(conditionResReg, thenBlock.blockId, elseBlock is null ? mergeBlock.blockId : elseBlock.blockId, ifStmt.location);

        SwitchCurrentBlock(thenBlock);
        BuildStmt(ifStmt.thenStmt);
        if (currentBlock.terminator is null) {
            TerminateGoto(mergeBlock.blockId, ifStmt.location);
        }

        if (elseBlock is not null) {
            SwitchCurrentBlock(elseBlock);
            BuildStmt(ifStmt.elseStmt!);
            if (currentBlock.terminator is null) {
                TerminateGoto(mergeBlock.blockId, ifStmt.location);
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
            TerminateGoto(condBlock.blockId, whileStmt.location);
        }

        SwitchCurrentBlock(condBlock);
        int resReg = BuildExpr(whileStmt.boundConditionExpr);
        TerminateBranch(resReg, whileBlock.blockId, endBlock.blockId, whileStmt.location);

        SwitchCurrentBlock(whileBlock);
        BuildStmt(whileStmt.body);

        if (currentBlock.terminator is null) {
            TerminateGoto(condBlock.blockId, whileStmt.location);
        }

        SwitchCurrentBlock(endBlock);
        loopTarget.Pop();
    }


    void BuildForStmt(BoundForStmt forStmt) {
        var condBlock = CreateBlock();
        var bodyBlock = CreateBlock();
        var iterBlock = CreateBlock();
        var endBlock = CreateBlock();

        loopTarget.Push(CreateLoopTarget(iterBlock, endBlock));

        BuildStmt(forStmt.initializer);

        if (currentBlock.terminator is null) {
            TerminateGoto(condBlock.blockId, forStmt.location);
        }

        SwitchCurrentBlock(condBlock);
        int condReg = BuildExpr(forStmt.condition);
        TerminateBranch(condReg, bodyBlock.blockId, endBlock.blockId, forStmt.location);

        SwitchCurrentBlock(bodyBlock);
        BuildStmt(forStmt.body);
        if (currentBlock.terminator is null) {
            TerminateGoto(iterBlock.blockId, forStmt.location);
        }

        SwitchCurrentBlock(iterBlock);
        BuildExpr(forStmt.iteration);
        if (currentBlock.terminator is null) {
            TerminateGoto(condBlock.blockId, forStmt.location);
        }

        SwitchCurrentBlock(endBlock);
        loopTarget.Pop();
    }
    void BuildExpressionStmt(BoundExpressionStmt expressionStmt) {
        BuildExpr(expressionStmt.boundExpr);
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
            case BoundVarAssignmentExpr va: {
                    return BuildVarAssignmentExpr(va);
                }
            case BoundLiteralExpr l: {
                    return BuildLiteralExpr(l);
                }
            case BoundNameExpr n: {
                    return BuildNameExpr(n);
                }
            case BoundBinaryExpr b: {
                    return BuildBinaryExpr(b);
                }
            case BoundUnaryExpr b: {
                    return BuildUnaryExpr(b);
                }
            case BoundCallExpr c: {
                    return BuildCallExpr(c);
                }
            default: {
                    throw new Exception("Unkown Expr in Build Expr Ir" + boundExpr);
                }
        }
    }
    int BuildVarAssignmentExpr(BoundVarAssignmentExpr assignmentStmt) {
        int srcReg = BuildExpr(assignmentStmt.assignmentExpr);
        var localIndex = assignmentStmt.localSymbol.index;
        EmitStoreLocal(srcReg, localIndex, assignmentStmt.location);
        return srcReg;
    }
    int BuildLiteralExpr(BoundLiteralExpr literalExpr) {
        var type = GetValueType(literalExpr.type);
        long value = literalExpr.value;
        return EmitLoadConst(type, value, literalExpr.location);
    }

    int BuildNameExpr(BoundNameExpr nameExpr) {
        int localIndex = nameExpr.localSymbol.index;
        return EmitLoadLocal(localIndex, nameExpr.location);
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

        return EmitBinary(irOpKind, leftReg, rightReg, binaryExpr.location);
    }


    int BuildUnaryExpr(BoundUnaryExpr unaryExpr) {
        var opKind = unaryExpr.boundUnaryOperator.unaryOperatorKind;
        var irUnaryOP = MapUnaryOp(opKind);

        int operandResReg = BuildExpr(unaryExpr.operatedExpr);

        return EmitUnary(irUnaryOP, operandResReg, unaryExpr.location);
    }

    int BuildCallExpr(BoundCallExpr callExpr) {
        List<int> argRegs = new();
        foreach (BoundExpr arg in callExpr.args) {
            argRegs.Add(BuildExpr(arg));
        }
        int functionInd = symbolToInd[callExpr.callee];
        return EmitCall(callExpr.argCount, argRegs.ToArray(), functionInd, callExpr.location);
    }

    int BuildLogicalOrExpr(BoundBinaryExpr binaryExpr) {
        int tempIndex = AllocTempLocalInd();

        int leftReg = BuildExpr(binaryExpr.leftBoundExpr);

        var rhsBlock = CreateBlock();
        var leftTrueBlock = CreateBlock();
        var mergeBlock = CreateBlock();

        TerminateBranch(leftReg, leftTrueBlock.blockId, rhsBlock.blockId, binaryExpr.location);

        SwitchCurrentBlock(leftTrueBlock);
        EmitStoreLocal(leftReg, tempIndex, binaryExpr.location);
        TerminateGoto(mergeBlock.blockId, binaryExpr.location);

        SwitchCurrentBlock(rhsBlock);
        int rightReg = BuildExpr(binaryExpr.rightBoundExpr);
        EmitStoreLocal(rightReg, tempIndex, binaryExpr.location);
        TerminateGoto(mergeBlock.blockId, binaryExpr.location);

        SwitchCurrentBlock(mergeBlock);

        int resReg = EmitLoadLocal(tempIndex, binaryExpr.location);

        return resReg;
    }

    int BuildLogicalAndExpr(BoundBinaryExpr binaryExpr) {
        int tempIndex = AllocTempLocalInd();

        int leftReg = BuildExpr(binaryExpr.leftBoundExpr);

        var falsyBranch = CreateBlock();
        var rhsBlock = CreateBlock();
        var mergeBlock = CreateBlock();

        TerminateBranch(leftReg, rhsBlock.blockId, falsyBranch.blockId, binaryExpr.location);

        SwitchCurrentBlock(falsyBranch);
        EmitStoreLocal(leftReg, tempIndex, binaryExpr.location);
        TerminateGoto(mergeBlock.blockId, binaryExpr.location);

        SwitchCurrentBlock(rhsBlock);
        int rightReg = BuildExpr(binaryExpr.rightBoundExpr);
        EmitStoreLocal(rightReg, tempIndex, binaryExpr.location);
        TerminateGoto(mergeBlock.blockId, binaryExpr.location);

        SwitchCurrentBlock(mergeBlock);

        int resReg = EmitLoadLocal(tempIndex, binaryExpr.location);

        return resReg;
    }


    void Emit(IrInstr irInstr) {
        if (currentBlock.terminator is not null) {
            throw new Exception("there is already a terminator in this block");
        }
        if (!currentBlock.location.IsValid) {
            currentBlock.location = irInstr.location;
        }
        currentBlock.irInstrs.Add(irInstr);
    }

    void Terminate(Terminator terminator) {
        if (currentBlock.terminator is not null) {
            throw new Exception("onyl one terminator allowed");
        }
        if (!currentBlock.location.IsValid) {
            currentBlock.location = terminator.location;
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

    int EmitLoadConst(Runtime.ValueType type, long value, SourceLocation location) {
        int dstReg = AllocVReg();
        Emit(new IrLoadConst(type, value, dstReg, location));
        return dstReg;
    }

    void EmitStoreLocal(int srcReg, int localIndex, SourceLocation location) {
        Emit(new IrStoreLocal(srcReg, localIndex, location));
    }

    int EmitLoadLocal(int localIndex, SourceLocation location) {
        int dstReg = AllocVReg();
        Emit(new IrLoadLocal(dstReg, localIndex, location));
        return dstReg;
    }

    int EmitMove(int dstReg, int srcReg, SourceLocation location) {
        Emit(new IrMove(dstReg, srcReg, location));
        return dstReg;
    }
    int EmitBinary(IrBinaryOPKind op, int leftReg, int rightReg, SourceLocation location) {
        int dstReg = AllocVReg();
        Emit(new IrBinaryOp(op, dstReg, leftReg, rightReg, location));
        return dstReg;
    }

    int EmitUnary(IrUnaryOpKind op, int srcReg, SourceLocation location) {
        int dstReg = AllocVReg();
        Emit(new IrUnary(dstReg, srcReg, op, location));
        return dstReg;
    }

    int EmitCall(int argCount, int[] argRegs, int functionIndex, SourceLocation location) {
        int dstReg = AllocVReg();
        Emit(new IrCallInstr(dstReg, argCount, argRegs, functionIndex, location));
        return dstReg;
    }
    void TerminateReturn(int returnReg, SourceLocation location) {
        Terminate(new IrReturn(returnReg, location));
    }


    void TerminateGoto(int targetBlockId, SourceLocation location) {
        Terminate(new IrGoto(targetBlockId, location));
    }


    void TerminateBranch(int condReg, int thenBlockId, int elseBlockId, SourceLocation location) {
        Terminate(new IrBranch(condReg, thenBlockId, elseBlockId, location));
    }

    LoopTarget CreateLoopTarget(BasicBlock condBlock, BasicBlock endBlock) {
        return new LoopTarget(condBlock.blockId, endBlock.blockId);
    }

    void StartFunction(BoundFunctionDeclaration functionDeclaration) {
        basicBlocks.Clear();
        loopTarget.Clear();
        nextBlockId = 0;
        nextVReg = 0;
        maxVReg = 0;
        nextTempLocalInd = functionDeclaration.functionSymbol.localCount;
        currentBlock = MakeNewBlock();
        basicBlocks.Add(currentBlock);
    }

    Runtime.ValueType GetValueType(TypeSymbol typeSymbol) {
        if (typeSymbol.IsSameType(BuiltInTypes.Int)) {
            return Runtime.ValueType.Int;
        }

        if (typeSymbol.IsSameType(BuiltInTypes.Bool)) {
            return Runtime.ValueType.Bool;
        }

        if (typeSymbol.IsSameType(BuiltInTypes.Char)) {
            return Runtime.ValueType.Char;
        }

        throw new Exception("Unkown symbol type in get value type" + typeSymbol);
    }


    IrBinaryOPKind MapBinaryOp(BoundBinaryOperatorKind kind) {
        return kind switch {
            BoundBinaryOperatorKind.AddInt => IrBinaryOPKind.AddInt,
            BoundBinaryOperatorKind.SubtractInt => IrBinaryOPKind.SubtractInt,
            BoundBinaryOperatorKind.MultiplyInt => IrBinaryOPKind.MultiplyInt,
            BoundBinaryOperatorKind.ModulusInt => IrBinaryOPKind.ModulusInt,
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


    IrUnaryOpKind MapUnaryOp(BoundUnaryOperatorKind kind) {
        return kind switch {
            BoundUnaryOperatorKind.LogicalNot => IrUnaryOpKind.Not,
            BoundUnaryOperatorKind.NegateInt => IrUnaryOpKind.NegInt,

            _ => throw new Exception("Unsupported unary operator: " + kind),
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
