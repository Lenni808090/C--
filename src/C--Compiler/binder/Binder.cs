
using System.ComponentModel;

class Binder {
    List<BoundStmt> boundStmts;
    Dictionary<string, LocalSymbol> localsByName;
    int nextLocalIndex;
    Stmt[] stmtsToBind;
    private static readonly Dictionary<string, SymbolType> standartTypes =
      new()
    {
        { "int", SymbolType.Int },
        { "bool", SymbolType.Bool },
    };
    public Binder(CompilationUnit compilationUnit) {
        localsByName = new();
        boundStmts = new();
        stmtsToBind = compilationUnit.stmts;
    }

    public BoundCompiledUnit BindCompiledUnit() {
        foreach (Stmt stmt in stmtsToBind) {
            var boundStmt = BindStmt(stmt);
            boundStmts.Add(boundStmt);
        }

        int localCount = localsByName.Count;
        return new BoundCompiledUnit(boundStmts.ToArray(), localCount);
    }


    BoundStmt BindStmt(Stmt stmt) {
        return stmt switch {
            VarDeclarationStmt v => BindVarDeclarationStmt(v),
            ReturnStmt r => BindReturnStmt(r),
            ExpressionStmt e => BindExpressionStmt(e),
            _ => throw new Exception($"Unexpected stmt: {stmt.syntaxKind}"),
        };
    }

    BoundStmt BindExpressionStmt(ExpressionStmt expressionStmt) {
        return new BoundExpressionStmt(BindExpr(expressionStmt.Expression));
    }

    BoundStmt BindReturnStmt(ReturnStmt returnStmt) {
        var boundReturnedExpr = BindExpr(returnStmt.returnExpr);
        return new BoundReturnStmt(boundReturnedExpr);
    }

    BoundStmt BindVarDeclarationStmt(VarDeclarationStmt varDeclarationStmt) {
        string name = varDeclarationStmt.name.Text;
        Token typeToken = ((IdentifierTypeSyntax)varDeclarationStmt.type).identifier;

        int index = nextLocalIndex++;

        SymbolType declared = InferTypeInTypedDecl(typeToken);
        LocalSymbol localSymbol = new LocalSymbol(name, declared, index);
        BoundExpr initBoundExpr = BindExpr(varDeclarationStmt.declarementExpr);

        if (declared != initBoundExpr.type) {
            throw new Exception("declared and assigned type are not the same");
        }

        if (localsByName.ContainsKey(varDeclarationStmt.name.Text)) {
            throw new Exception("var already declared in this scope");
        }

        localsByName.Add(varDeclarationStmt.name.Text, localSymbol);
        return new BoundVarDeclarationStmt(localSymbol, initBoundExpr);
    }

    BoundExpr BindExpr(Expr expr) {
        return expr switch {
            NameExpr n => BindNameExpr(n),
            LiteralExpr l => BindLiteralExpr(l),
            BinaryExpr b => BindBinaryExpr(b),
            _ => throw new Exception($"Unexpected expr: {expr.syntaxKind}"),
        };
    }


    BoundExpr BindNameExpr(NameExpr nameExpr) {
        string name = nameExpr.name.Text;
        if (localsByName.TryGetValue(name, out LocalSymbol? local)) {
            return new BoundNameExpr(local!);
        }
        else {
            throw new Exception("this var is not declared");
        }
    }

    BoundExpr BindLiteralExpr(LiteralExpr literalExpr) {
        TokenType tokenType = literalExpr.value.TokenType;
        SymbolType type = InferType(tokenType);

        if (type == SymbolType.Int) {
            if (!literalExpr.value.hasValue) {
                throw new Exception("Number literal needs to have a value");
            }

            long v = literalExpr.value.Value;
            return new BoundLiteralExpr(v, type);
        }

        if (type == SymbolType.Bool) {
            long v = tokenType == TokenType.True ? 1 : 0;
            return new BoundLiteralExpr(v, type);
        }

        throw new Exception("Unexpected literal type: " + type);
    }

    BoundExpr BindBinaryExpr(BinaryExpr binaryExpr) {
        BoundExpr boundLeftExpr = BindExpr(binaryExpr.leftExpr);
        BoundExpr boundRightExpr = BindExpr(binaryExpr.rightExpr);
        if (boundLeftExpr.type != boundRightExpr.type) {
            throw new Exception("both expressions need too have the same type");
        }
        var op = binaryExpr.Operator.TokenType;
        BoundBinaryOperatorKind boundBinaryOperatorKind = InferBinaryOperatorKind(op, boundLeftExpr.type);
        return new BoundBinaryExpr(boundLeftExpr, boundRightExpr, boundBinaryOperatorKind, boundLeftExpr.type);
    }

    SymbolType InferTypeInTypedDecl(Token typeToken) {
        if (standartTypes.TryGetValue(typeToken.Text, out SymbolType type)) {
            return type;
        }
        else {
            throw new Exception("Unknown Type " + typeToken.Text);
        }
    }

    SymbolType InferType(TokenType tokenType) {
        switch (tokenType) {
            case TokenType.True:
            case TokenType.False: {
                    return SymbolType.Bool;
                }
            case TokenType.Number: {
                    return SymbolType.Int;
                }
            default: {
                    throw new Exception("unkown type " + tokenType);
                }
        }

    }

    BoundBinaryOperatorKind InferBinaryOperatorKind(TokenType op, SymbolType mainBinaryType) {
        if (mainBinaryType == SymbolType.Int) {
            return op switch {
                TokenType.Plus => BoundBinaryOperatorKind.AddInt,
                TokenType.Minus => BoundBinaryOperatorKind.SubtractInt,
                TokenType.Multiply => BoundBinaryOperatorKind.MultiplyInt,
                TokenType.Divide => BoundBinaryOperatorKind.DivideInt,
                _ => throw new Exception("unkown int binary operator" + op),
            };
        }
        else {
            throw new Exception("unkown binary operator for this type" + op);
        }
    }

}