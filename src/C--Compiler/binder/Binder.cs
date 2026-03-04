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
        return new BoundExpressionStmt(BindExpr(expressionStmt.Expression), GetStmtSpan(expressionStmt));
    }

    BoundStmt BindReturnStmt(ReturnStmt returnStmt) {
        var boundReturnedExpr = BindExpr(returnStmt.returnExpr);
        return new BoundReturnStmt(boundReturnedExpr, GetStmtSpan(returnStmt));
    }

    BoundStmt BindIfStmt(IfStmt ifStmt) {
        BoundExpr boundConditionExpr = BindExpr(ifStmt.condition);

        if (boundConditionExpr.type != SymbolType.Bool && boundConditionExpr.type != SymbolType.DiagnosticsError) {
            ReportError(DiagnosticDescriptors.BinderConditionMustBeBool, GetExprSpan(ifStmt.condition));
        }

        BoundStmt thenStmt = BindStmt(ifStmt.thenStmt);

        if (ifStmt.elseStmt is null) {
            return new BoundIfStmt(boundConditionExpr, thenStmt, GetStmtSpan(ifStmt));
        }

        BoundStmt elseStmt = BindStmt(ifStmt.elseStmt);

        return new BoundIfStmt(boundConditionExpr, thenStmt, GetStmtSpan(ifStmt), elseStmt);
    }

    BoundStmt BindBlockStmt(BlockStmt blockStmt) {
        PushScope();
        List<BoundStmt> boundStmts = new();

        foreach (Stmt stmt in blockStmt.stmts) {
            boundStmts.Add(BindStmt(stmt));
        }

        PopScope();
        return new BoundBlockStmt(boundStmts.ToArray(), GetStmtSpan(blockStmt));
    }
    BoundStmt BindVarDeclarationStmt(VarDeclarationStmt varDeclarationStmt) {
        string name = varDeclarationStmt.name.Text;
        Token typeToken = ((IdentifierTypeSyntax)varDeclarationStmt.type).identifier;
        bool isAlreadyInScope = scopes.Peek().ContainsKey(varDeclarationStmt.name.Text);
        if (isAlreadyInScope) {
            ReportError(DiagnosticDescriptors.BinderVarAlreadyDeclared, varDeclarationStmt.name.TextSpan, name);
        }

        SymbolType declared = InferTypeInTypedDecl(typeToken);
        BoundExpr initBoundExpr = BindExpr(varDeclarationStmt.declarementExpr);

        if (declared != initBoundExpr.type && declared != SymbolType.DiagnosticsError && initBoundExpr.type != SymbolType.DiagnosticsError) {
            ReportError(DiagnosticDescriptors.BinderDeclaredAndAssignedTypeMismatch, varDeclarationStmt.name.TextSpan);
        }

        int index = AllocateLocalIndex();
        LocalSymbol localSymbol = new LocalSymbol(name, declared, index);
        if (!isAlreadyInScope) {
            scopes.Peek().Add(varDeclarationStmt.name.Text, localSymbol);
        }

        return new BoundVarDeclarationStmt(localSymbol, initBoundExpr, GetStmtSpan(varDeclarationStmt));
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
        ReportError(DiagnosticDescriptors.BinderVariableNotDeclared, nameExpr.name.TextSpan, name);
        return new BoundErrorExpr();

    }

    BoundExpr BindLiteralExpr(LiteralExpr literalExpr) {
        Token literalToken = literalExpr.value;
        TokenType tokenType = literalToken.TokenType;
        SymbolType type = InferType(literalToken);

        if (type == SymbolType.Int) {
            if (!literalExpr.value.hasValue) {
                ReportError(DiagnosticDescriptors.BinderNumberLiteralMissingValue, literalToken.TextSpan);
                return new BoundErrorExpr();
            }

            long v = literalExpr.value.Value;
            return new BoundLiteralExpr(v, type);
        }

        if (type == SymbolType.Bool) {
            long v = tokenType == TokenType.True ? 1 : 0;
            return new BoundLiteralExpr(v, type);
        }

        ReportError(DiagnosticDescriptors.BinderUnexpectedLiteralType, literalToken.TextSpan, type);
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

        ReportError(DiagnosticDescriptors.BinderBinaryTypeMismatch, binaryExpr.Operator.TextSpan);
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

        ReportError(DiagnosticDescriptors.BinderUnknownTypeToken, typeToken.TextSpan, typeToken.Text);
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
                    ReportError(DiagnosticDescriptors.BinderUnknownTokenType, token.TextSpan, tokenType);
                    return SymbolType.DiagnosticsError;
                }
        }

    }

    TextSpan GetExprSpan(Expr expr) {
        return expr switch {
            LiteralExpr l => l.value.TextSpan,
            NameExpr n => n.name.TextSpan,
            BinaryExpr b => b.Operator.TextSpan,
            _ => TextSpan.None,
        };
    }

    TextSpan GetStmtSpan(Stmt stmt) {
        return stmt switch {
            VarDeclarationStmt v => CombineSpans(GetTypeSpan(v.type), GetExprSpan(v.declarementExpr)),
            ReturnStmt r => GetExprSpan(r.returnExpr),
            IfStmt i => i.elseStmt is null
                ? CombineSpans(GetExprSpan(i.condition), GetStmtSpan(i.thenStmt))
                : CombineSpans(GetExprSpan(i.condition), GetStmtSpan(i.elseStmt!)),
            BlockStmt b => GetBlockSpan(b),
            ExpressionStmt e => GetExprSpan(e.Expression),
            _ => TextSpan.None,
        };
    }

    TextSpan GetBlockSpan(BlockStmt blockStmt) {
        if (blockStmt.stmts.Length == 0) {
            return TextSpan.None;
        }

        TextSpan first = GetStmtSpan(blockStmt.stmts[0]);
        TextSpan last = GetStmtSpan(blockStmt.stmts[blockStmt.stmts.Length - 1]);
        return CombineSpans(first, last);
    }

    TextSpan GetTypeSpan(TypeSyntax typeSyntax) {
        return typeSyntax switch {
            IdentifierTypeSyntax i => i.identifier.TextSpan,
            _ => TextSpan.None,
        };
    }

    TextSpan CombineSpans(TextSpan first, TextSpan second) {
        if (first.Length == 0) {
            return second;
        }

        if (second.Length == 0) {
            return first;
        }

        int start = Math.Min(first.Start, second.Start);
        int end = Math.Max(first.Start + first.Length, second.Start + second.Length);
        return new TextSpan(start, end - start);
    }

    void ReportError(DiagnosticDescriptor descriptor, TextSpan textSpan, params object[] args) {
        diagnostics.Report(descriptor, textSpan, args);
    }
}

