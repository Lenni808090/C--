class CodeGenerator {
    FunctionBuilder functionBuilder;
    BoundCompiledUnit boundCompiledUnit;

    readonly Dictionary<BoundBinaryOperatorKind, Action<byte, byte, byte>> binaryEmitters;
    byte nextReg;
    public CodeGenerator(BoundCompiledUnit boundCompiledUnit) {
        functionBuilder = new();
        binaryEmitters = new() {
            { BoundBinaryOperatorKind.AddInt, functionBuilder.Emitter.EmitAddInt },
            { BoundBinaryOperatorKind.SubtractInt, functionBuilder.Emitter.EmitSubtractInt },
            { BoundBinaryOperatorKind.MultiplyInt, functionBuilder.Emitter.EmitMultiplyInt },
            { BoundBinaryOperatorKind.DivideInt, functionBuilder.Emitter.EmitDivideInt },
        };
        this.boundCompiledUnit = boundCompiledUnit;
    }

    public CompiledFunction GenerateFunction() {
        foreach (BoundStmt boundStmt in boundCompiledUnit.boundStmts) {
            EmitStmt(boundStmt);
        }
        int localCount = boundCompiledUnit.localCount;
        return functionBuilder.Build(localCount);
    }


    void EmitStmt(BoundStmt boundStmt) {
        switch (boundStmt) {
            case BoundVarDeclarationStmt declarationStmt: {
                    EmitVarDeclaration(declarationStmt);
                    break;
                }
            case BoundReturnStmt returnStmt: {
                    EmitReturnStmt(returnStmt);
                    break;
                }
            case BoundExpressionStmt stmt: {
                    EmitExpresssionStmt(stmt);
                    break;
                }
            default: {
                    throw new Exception("unkown stmt in codegen: " + boundStmt);
                }
        }
        nextReg = 0;
    }

    void EmitVarDeclaration(BoundVarDeclarationStmt boundVarDeclarationStmt) {
        int localIndex = boundVarDeclarationStmt.localSymbol.index;
        byte srcReg = EmitExpr(boundVarDeclarationStmt.initializer);
        functionBuilder.Emitter.EmitStoreLocal(srcReg, (byte)localIndex);
    }

    void EmitReturnStmt(BoundReturnStmt boundReturnStmt) {
        byte srcReg = EmitExpr(boundReturnStmt.boundReturnedExpr);
        functionBuilder.Emitter.EmitReturn(srcReg);
    }

    void EmitExpresssionStmt(BoundExpressionStmt boundExpressionStmt) {
        EmitExpr(boundExpressionStmt.boundExpr);
    }

    byte EmitExpr(BoundExpr boundExpr) {
        return boundExpr switch {
            BoundLiteralExpr boundLiteralExpr => EmitLiteralExpr(boundLiteralExpr),
            BoundNameExpr boundNameExpr => EmitNameExpr(boundNameExpr),
            BoundBinaryExpr boundBinaryExpr => EmitBinaryExpr(boundBinaryExpr),
            _ => throw new Exception("unkown expression in emit expr"),

        };
    }

    byte EmitLiteralExpr(BoundLiteralExpr literalExpr) {
        byte dstReg = AllocReg();
        long value = literalExpr.value;
        ValueType type = getValueType(literalExpr.type);

        var newConst = new Value(type, value);
        int constIndex = functionBuilder.AddConstant(newConst);
        functionBuilder.Emitter.EmitLoadConstant(dstReg, (byte)constIndex);
        return dstReg;
    }

    byte EmitNameExpr(BoundNameExpr nameExpr) {
        byte dstReg = AllocReg();
        int localIndex = nameExpr.localSymbol.index;
        functionBuilder.Emitter.EmitLoadLocal(dstReg, (byte)localIndex);
        return dstReg;
    }

    byte EmitBinaryExpr(BoundBinaryExpr binaryExpr) {
        byte leftDstReg = EmitExpr(binaryExpr.leftBoundExpr);
        byte rightDstReg = EmitExpr(binaryExpr.rightBoundExpr);

        byte resDstReg = AllocReg();
        var opKind = binaryExpr.boundBinaryOperatorKind;
        if (!binaryEmitters.TryGetValue(opKind, out var emit)) {
            throw new Exception("unkown binary op" + opKind);
        }

        emit(resDstReg, leftDstReg, rightDstReg);
        return resDstReg;
    }
    ValueType getValueType(SymbolType symbolType) {
        return symbolType switch {
            SymbolType.Int => ValueType.Int,
            SymbolType.Bool => ValueType.Bool,
            _ => throw new Exception("Unkown symbol type in get value type" + symbolType),
        };
    }

    byte AllocReg() {
        return nextReg++;
    }

}