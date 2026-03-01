using CMinus.Compiler;
using CMinus.Compiler.Syntax;

namespace CMinus.Compiler.Binding;

class Binder {
    List<BoundStmt> boundStmts;
    Stack<Dictionary<string, LocalSymbol>> scopes;


    //!!!!!!!!!!!!!!! temporary return will expand later!!!!!!!!!!!!!!!!!!
    bool hasReturn;
    int nextLocalIndex;
    Stmt[] stmtsToBind;
    private static readonly Dictionary<string, SymbolType> standartTypes =
      new()
    {
        { "int", SymbolType.Int },
        { "bool", SymbolType.Bool },
    };


    public Binder(CompilationUnit compilationUnit) {
        scopes = new();
        boundStmts = new();
        stmtsToBind = compilationUnit.stmts;
    }

    public BoundCompiledUnit BindCompiledUnit() {
        PushScope();

        foreach (Stmt stmt in stmtsToBind) {
            var boundStmt = BindStmt(stmt);
            if (boundStmt is BoundReturnStmt) {
                hasReturn = true;
            }
            boundStmts.Add(boundStmt);
        }
        PopScope();
        if (!hasReturn) {
            boundStmts.Add(
                new BoundReturnStmt(
                    new BoundLiteralExpr(0, SymbolType.Int)
                )
            );
        }

        int localCount = nextLocalIndex;
        return new BoundCompiledUnit(boundStmts.ToArray(), localCount);
    }


    BoundStmt BindStmt(Stmt stmt) {
        return stmt switch {
            VarDeclarationStmt v => BindVarDeclarationStmt(v),
            ReturnStmt r => BindReturnStmt(r),
            IfStmt i => BindIfStmt(i),
            BlockStmt b => BindBlockStmt(b),
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

    BoundStmt BindIfStmt(IfStmt ifStmt) {
        BoundExpr boundConditionExpr = BindExpr(ifStmt.condition);

        if (boundConditionExpr.type != SymbolType.Bool) {
            throw new Exception("condition must be of type bool");
        }

        BoundStmt thenStmt = BindStmt(ifStmt.thenStmt);

        return new BoundIfStmt(boundConditionExpr, thenStmt);
    }

    BoundStmt BindBlockStmt(BlockStmt blockStmt) {
        PushScope();
        List<BoundStmt> boundStmts = new();

        foreach (Stmt stmt in blockStmt.stmts) {
            boundStmts.Add(BindStmt(stmt));
        }

        PopScope();
        return new BoundBlockStmt(boundStmts.ToArray());
    }
    BoundStmt BindVarDeclarationStmt(VarDeclarationStmt varDeclarationStmt) {
        string name = varDeclarationStmt.name.Text;
        Token typeToken = ((IdentifierTypeSyntax)varDeclarationStmt.type).identifier;

        if (scopes.Peek().ContainsKey(varDeclarationStmt.name.Text)) {
            throw new Exception("var already declared in this scope");
        }

        int index = nextLocalIndex++;

        SymbolType declared = InferTypeInTypedDecl(typeToken);
        LocalSymbol localSymbol = new LocalSymbol(name, declared, index);
        BoundExpr initBoundExpr = BindExpr(varDeclarationStmt.declarementExpr);

        if (declared != initBoundExpr.type) {
            throw new Exception("declared and assigned type are not the same");
        }

        scopes.Peek().Add(varDeclarationStmt.name.Text, localSymbol);
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
        LocalSymbol? localSymbol = lookUpLocal(name);
        if (localSymbol is not null) {
            return new BoundNameExpr(localSymbol);
        }
        throw new Exception("this var is not declared" + name);

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

        var op = binaryExpr.Operator.TokenType;
        BoundBinaryOperator? boundBinaryOperator = BoundBinaryOperator.GetBinaryOperator(op, boundLeftExpr.type, boundRightExpr.type);
        if (boundBinaryOperator is not null) {
            return new BoundBinaryExpr(boundLeftExpr, boundRightExpr, boundBinaryOperator, boundBinaryOperator.resultType);
        }
        else {
            throw new Exception("type mismatch in binary operation");
        }

    }

    LocalSymbol? lookUpLocal(string name) {
        foreach (var scope in scopes) {
            if (scope.TryGetValue(name, out LocalSymbol? localSymbol)) {
                return localSymbol;
            }
        }
        return null;
    }
    void PushScope() {
        scopes.Push(new Dictionary<string, LocalSymbol>());
    }

    void PopScope() {
        scopes.Pop();
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
}

