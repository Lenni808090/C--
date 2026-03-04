using CMinus.Compiler;
using CMinus.Compiler.Diagnostics;
using CMinus.Compiler.Syntax;

namespace CMinus.Compiler.Binding;

class Binder {
    List<BoundStmt> boundStmts;
    Stack<Dictionary<string, LocalSymbol>> scopes;

    DiagnosticBag diagnostics;

    int nextLocalIndex;
    Stmt[] stmtsToBind;
    private static readonly Dictionary<string, SymbolType> standartTypes =
      new()
    {
        { "int", SymbolType.Int },
        { "bool", SymbolType.Bool },
    };


    public Binder(CompilationUnit compilationUnit, CompilerContext compilerContext) {
        scopes = new();
        diagnostics = compilerContext.diagnostics;
        boundStmts = new();
        stmtsToBind = compilationUnit.stmts;
    }

    public BoundCompiledUnit BindCompiledUnit() {
        PushScope();

        foreach (Stmt stmt in stmtsToBind) {
            var boundStmt = BindStmt(stmt);
            boundStmts.Add(boundStmt);
        }
        PopScope();

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

        if (boundConditionExpr.type != SymbolType.Bool && boundConditionExpr.type != SymbolType.DiagnosticsError) {
            ReportError(DiagnosticDescriptors.BinderConditionMustBeBool);
        }

        BoundStmt thenStmt = BindStmt(ifStmt.thenStmt);

        if (ifStmt.elseStmt is null) {
            return new BoundIfStmt(boundConditionExpr, thenStmt);
        }

        BoundStmt elseStmt = BindStmt(ifStmt.elseStmt);

        return new BoundIfStmt(boundConditionExpr, thenStmt, elseStmt);
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
        bool isAlreadyInScope = scopes.Peek().ContainsKey(varDeclarationStmt.name.Text);
        if (isAlreadyInScope) {
            ReportError(DiagnosticDescriptors.BinderVarAlreadyDeclared, name);
        }

        SymbolType declared = InferTypeInTypedDecl(typeToken);
        BoundExpr initBoundExpr = BindExpr(varDeclarationStmt.declarementExpr);

        if (declared != initBoundExpr.type && declared != SymbolType.DiagnosticsError && initBoundExpr.type != SymbolType.DiagnosticsError) {
            ReportError(DiagnosticDescriptors.BinderDeclaredAndAssignedTypeMismatch);
        }

        int index = AllocateLocalIndex();
        LocalSymbol localSymbol = new LocalSymbol(name, declared, index);
        if (!isAlreadyInScope) {
            scopes.Peek().Add(varDeclarationStmt.name.Text, localSymbol);
        }

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
        ReportError(DiagnosticDescriptors.BinderVariableNotDeclared, name);
        return new BoundErrorExpr();

    }

    BoundExpr BindLiteralExpr(LiteralExpr literalExpr) {
        Token literalToken = literalExpr.value;
        TokenType tokenType = literalToken.TokenType;
        SymbolType type = InferType(literalToken);

        if (type == SymbolType.Int) {
            if (!literalExpr.value.hasValue) {
                ReportError(DiagnosticDescriptors.BinderNumberLiteralMissingValue);
                return new BoundErrorExpr();
            }

            long v = literalExpr.value.Value;
            return new BoundLiteralExpr(v, type);
        }

        if (type == SymbolType.Bool) {
            long v = tokenType == TokenType.True ? 1 : 0;
            return new BoundLiteralExpr(v, type);
        }

        ReportError(DiagnosticDescriptors.BinderUnexpectedLiteralType, type);
        return new BoundErrorExpr();
    }

    BoundExpr BindBinaryExpr(BinaryExpr binaryExpr) {
        BoundExpr boundLeftExpr = BindExpr(binaryExpr.leftExpr);
        BoundExpr boundRightExpr = BindExpr(binaryExpr.rightExpr);

        var op = binaryExpr.Operator.TokenType;
        BoundBinaryOperator? boundBinaryOperator = BoundBinaryOperator.GetBinaryOperator(op, boundLeftExpr.type, boundRightExpr.type);
        if (boundBinaryOperator is not null) {
            return new BoundBinaryExpr(boundLeftExpr, boundRightExpr, boundBinaryOperator, boundBinaryOperator.resultType);
        }

        if (boundLeftExpr.type == SymbolType.DiagnosticsError || boundRightExpr.type == SymbolType.DiagnosticsError) {
            return new BoundErrorExpr();
        }

        ReportError(DiagnosticDescriptors.BinderBinaryTypeMismatch);
        return new BoundErrorExpr();

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

    int AllocateLocalIndex() {
        return nextLocalIndex++;
    }

    SymbolType InferTypeInTypedDecl(Token typeToken) {
        if (standartTypes.TryGetValue(typeToken.Text, out SymbolType type)) {
            return type;
        }

        ReportError(DiagnosticDescriptors.BinderUnknownTypeToken, typeToken.Text);
        return SymbolType.DiagnosticsError;
    }

    SymbolType InferType(Token token) {
        TokenType tokenType = token.TokenType;
        switch (tokenType) {
            case TokenType.True:
            case TokenType.False: {
                    return SymbolType.Bool;
                }
            case TokenType.Number: {
                    return SymbolType.Int;
                }
            default: {
                    ReportError(DiagnosticDescriptors.BinderUnknownTokenType, tokenType);
                    return SymbolType.DiagnosticsError;
                }
        }

    }

    void ReportError(DiagnosticDescriptor descriptor, params object[] args) {
        diagnostics.Report(descriptor, args);
    }
}

