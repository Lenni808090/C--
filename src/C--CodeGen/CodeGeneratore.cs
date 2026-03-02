using CMinus.Compiler.Binding;
using CMinus.Compiler.Syntax;
using CMinus.Runtime;

namespace CMinus.CodeGen;

class CodeGenerator {
    FunctionBuilder functionBuilder;
    BoundCompiledUnit boundCompiledUnit;


    byte nextReg;
    byte maxReg;
    public CodeGenerator(BoundCompiledUnit boundCompiledUnit) {
        functionBuilder = new();
        this.boundCompiledUnit = boundCompiledUnit;
    }

    public CompiledFunction GenerateFunction() {
        nextReg = 0;
        foreach (BoundStmt boundStmt in boundCompiledUnit.boundStmts) {
            EmitStmt(boundStmt);
        }
        int localCount = boundCompiledUnit.localCount;
        return functionBuilder.Build(localCount, maxReg);
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
            case BoundIfStmt boundIfStmt: {
                    EmitIfStmt(boundIfStmt);
                    break;
                }
            case BoundBlockStmt boundBlockStmt: {
                    EmitBlockStmt(boundBlockStmt);
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

    void EmitIfStmt(BoundIfStmt boundIfStmt) {
        byte condReg = EmitExpr(boundIfStmt.boundConditionExpr);

        var newLabel = functionBuilder.Emitter.NewLabel();
        functionBuilder.Emitter.EmitJumpIfFalse(condReg, newLabel);

        EmitStmt(boundIfStmt.thenStmt);

        functionBuilder.Emitter.DefineLabel(newLabel);
    }

    void EmitBlockStmt(BoundBlockStmt boundBlockStmt) {
        foreach (BoundStmt boundStmt in boundBlockStmt.boundStmts) {
            EmitStmt(boundStmt);
        }
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
        Runtime.ValueType type = getValueType(literalExpr.type);

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
        var opKind = binaryExpr.boundBinaryOperator.operatorKind;
        var emit = BinaryOperatorEmitter.getEmitMethod(opKind);
        if (emit is not null) {
            emit(functionBuilder.Emitter, resDstReg, leftDstReg, rightDstReg);
        }
        else {
            throw new Exception("unkown binary operator in codegen " + opKind);
        }

        return resDstReg;
    }
    Runtime.ValueType getValueType(SymbolType symbolType) {
        return symbolType switch {
            SymbolType.Int => Runtime.ValueType.Int,
            SymbolType.Bool => Runtime.ValueType.Bool,
            _ => throw new Exception("Unkown symbol type in get value type" + symbolType),
        };
    }

    byte AllocReg() {
        byte reg = nextReg++;
        if (nextReg > maxReg) {
            maxReg = nextReg;
        }
        return reg;
    }

}
