using CMinus.Compiler.Binding;

namespace CMinus.Compiler.Lowering;

class IrBuilder {
    List<BasicBlock> basicBlocks;
    Stack<LoopTarget> loopTarget;
    RuntimeTypeInterner runtimeTypeInterner;
    ConstantInterner constantInterner;

    Dictionary<FunctionSymbol, int> symbolToInd;
    Dictionary<LocalSymbol, int> localSlots;
    BasicBlock currentBlock;

    BoundCompiledUnit boundCompiledUnit;
    int nextBlockId;
    int nextLocalSlot;
    int nextVReg;
    int maxVReg;

    public IrBuilder(BoundCompiledUnit boundCompiledUnit) {
        symbolToInd = new();
        basicBlocks = new();
        loopTarget = new();
        runtimeTypeInterner = new();
        constantInterner = new();
        localSlots = new();
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

        return new IrCompiledUnit(builtFunctions.ToArray(), mainFunctionInd, runtimeTypeInterner.BuildTypeTable(), constantInterner.Build());
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
        var irFunc = new IrFunction(basicBlocks.ToArray(), nextLocalSlot, maxVReg, functionDeclaration.functionSymbol.argCount);

        return irFunc;
    }

    void BuildStmt(BoundStmt boundStmt) {
        EnsureCurrentBlockLocation(boundStmt.location);
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
        int localIndex = nextLocalSlot++;
        localSlots[v.localSymbol] = localIndex;
        int srcReg = BuildExpr(v.initializer);
        EmitStoreLocal(srcReg, localIndex);
    }

    void BuildContinueStmt(BoundContinueStmt continueStmt) {
        var condBlockId = loopTarget.Peek().continueBlockId;
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


    void BuildForStmt(BoundForStmt forStmt) {
        var condBlock = CreateBlock();
        var bodyBlock = CreateBlock();
        var iterBlock = CreateBlock();
        var endBlock = CreateBlock();

        loopTarget.Push(CreateLoopTarget(iterBlock, endBlock));

        BuildStmt(forStmt.initializer);

        if (currentBlock.terminator is null) {
            TerminateGoto(condBlock.blockId);
        }

        SwitchCurrentBlock(condBlock);
        int condReg = BuildExpr(forStmt.condition);
        TerminateBranch(condReg, bodyBlock.blockId, endBlock.blockId);

        SwitchCurrentBlock(bodyBlock);
        BuildStmt(forStmt.body);
        if (currentBlock.terminator is null) {
            TerminateGoto(iterBlock.blockId);
        }

        SwitchCurrentBlock(iterBlock);
        BuildExpr(forStmt.iteration);
        if (currentBlock.terminator is null) {
            TerminateGoto(condBlock.blockId);
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
        EnsureCurrentBlockLocation(boundExpr.location);
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
            case BoundArrayCreationExpr a: {
                    return BuildArrayCreationExpr(a);
                }
            case BoundIndexExpr i: {
                    return BuildIndexExpr(i);
                }
            case BoundIndexAssignmentExpr i: {
                    return BuildIndexAssignmentExpr(i);
                }
            default: {
                    throw new Exception("Unkown Expr in Build Expr Ir" + boundExpr);
                }
        }
    }
    int BuildVarAssignmentExpr(BoundVarAssignmentExpr assignmentStmt) {
        int srcReg = BuildExpr(assignmentStmt.value);
        EmitStoreLocal(srcReg, localSlots[assignmentStmt.localSymbol]);
        return srcReg;
    }
    int BuildLiteralExpr(BoundLiteralExpr literalExpr) {
        var type = GetValueType(literalExpr.type);
        long value = literalExpr.value;
        int constIndex = constantInterner.Intern(type, value);
        return EmitLoadConst(constIndex, type, value);
    }

    int BuildNameExpr(BoundNameExpr nameExpr) {
        return EmitLoadLocal(localSlots[nameExpr.localSymbol]);
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


    int BuildUnaryExpr(BoundUnaryExpr unaryExpr) {
        var opKind = unaryExpr.boundUnaryOperator.unaryOperatorKind;
        var irUnaryOP = MapUnaryOp(opKind);

        int operandResReg = BuildExpr(unaryExpr.operatedExpr);

        return EmitUnary(irUnaryOP, operandResReg);
    }

    int BuildCallExpr(BoundCallExpr callExpr) {
        List<int> argRegs = new();
        foreach (BoundExpr arg in callExpr.args) {
            argRegs.Add(BuildExpr(arg));
        }
        int functionInd = symbolToInd[callExpr.callee];
        return EmitCall(callExpr.argCount, argRegs.ToArray(), functionInd);
    }

    int BuildArrayCreationExpr(BoundArrayCreationExpr arrayCreationExpr) {
        int lengthReg = BuildExpr(arrayCreationExpr.length);
        int typeId = GetRuntimeTypeId(arrayCreationExpr.type);
        return EmitNewArray(typeId, lengthReg);
    }

    int BuildIndexExpr(BoundIndexExpr indexExpr) {
        int arrayReg = BuildExpr(indexExpr.target);
        int indexReg = BuildExpr(indexExpr.index);
        return EmitLoadElement(arrayReg, indexReg);
    }

    int BuildIndexAssignmentExpr(BoundIndexAssignmentExpr expr) {
        int arrayReg = BuildExpr(expr.target);
        int indexReg = BuildExpr(expr.index);

        if (!expr.isCompound) {
            int valueReg = BuildExpr(expr.value);
            EmitStoreElement(valueReg, arrayReg, indexReg);
            return valueReg;
        }

        int oldElementReg = EmitLoadElement(arrayReg, indexReg);
        int rhsReg = BuildExpr(expr.value);
        int resReg = EmitBinary(MapBinaryOp(expr.op!.operatorKind), oldElementReg, rhsReg);
        EmitStoreElement(resReg, arrayReg, indexReg);

        return resReg;
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

    void EnsureCurrentBlockLocation(SourceLocation location) {
        if (!currentBlock.location.IsValid) {
            currentBlock.location = location;
        }
    }

    int AllocTempLocalInd() {
        return nextLocalSlot++;
    }

    int EmitLoadConst(int constIndex, Runtime.ValueType type, long value) {
        int dstReg = AllocVReg();
        Emit(new IrLoadConst(constIndex, dstReg));
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

    int EmitUnary(IrUnaryOpKind op, int srcReg) {
        int dstReg = AllocVReg();
        Emit(new IrUnary(dstReg, srcReg, op));
        return dstReg;
    }

    int EmitCall(int argCount, int[] argRegs, int functionIndex) {
        int dstReg = AllocVReg();
        Emit(new IrCallInstr(dstReg, argCount, argRegs, functionIndex));
        return dstReg;
    }

    int EmitNewArray(int typeId, int lengthReg) {
        int dstReg = AllocVReg();
        Emit(new IrNewArray(dstReg, typeId, lengthReg));
        return dstReg;
    }

    void EmitStoreElement(int srcReg, int arrayReg, int indexReg) {
        Emit(new IrStoreElement(srcReg, arrayReg, indexReg));
    }

    int EmitLoadElement(int arrayReg, int indexReg) {
        int dstReg = AllocVReg();
        Emit(new IrLoadElement(dstReg, arrayReg, indexReg));
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

    void StartFunction(BoundFunctionDeclaration functionDeclaration) {
        basicBlocks.Clear();
        loopTarget.Clear();
        localSlots = new();
        nextBlockId = 0;
        nextLocalSlot = 0;
        nextVReg = 0;
        maxVReg = 0;

        foreach (var param in functionDeclaration.paramSymbols) {
            localSlots[param] = nextLocalSlot++;
        }

        currentBlock = MakeNewBlock();
        currentBlock.location = functionDeclaration.location;
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

    int GetRuntimeTypeId(TypeSymbol typeSymbol) {
        return runtimeTypeInterner.Intern(typeSymbol);
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
