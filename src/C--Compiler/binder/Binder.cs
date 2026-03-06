using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using CMinus.Compiler;
using CMinus.Compiler.Diagnostics;
using CMinus.Compiler.Syntax;

namespace CMinus.Compiler.Binding;

class Binder {
    List<BoundStmt> boundStmts;
    Stack<Dictionary<string, LocalSymbol>> scopes;

    int loopDepth;
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
        loopDepth = 0;
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
            ContinueStmt c => BindContinueStmt(c),
            BreakStmt b => BindBreakStmt(b),
            WhileStmt w => BindWhileStmt(w),
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

    BoundStmt BindContinueStmt(ContinueStmt continueStmt) {
        if (!isInLoop()) {
            ReportError(DiagnosticDescriptors.BinderNotInLoopContinue);
        }
        return new BoundContinueStmt();
    }


    BoundStmt BindBreakStmt(BreakStmt breakStmt) {
        if (!isInLoop()) {
            ReportError(DiagnosticDescriptors.BinderNotInLoopBreak);
        }
        return new BoundBreakStmt();
    }
    BoundStmt BindWhileStmt(WhileStmt whileStmt) {

        BoundExpr boundConditionExpr = BindExpr(whileStmt.condition);

        if (boundConditionExpr.type != SymbolType.Bool && boundConditionExpr.type != SymbolType.DiagnosticsError) {
            ReportError(DiagnosticDescriptors.BinderConditionMustBeBool);
        }

        EnterLoop();
        BoundStmt body = BindStmt(whileStmt.body);
        ExitLoop();

        return new BoundWhileStmt(boundConditionExpr, body);
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


        BoundExpr initBoundExpr = BindExpr(varDeclarationStmt.declarementExpr);

        bool isAlreadyInScope = scopes.Peek().ContainsKey(name);
        if (isAlreadyInScope) {
            ReportError(DiagnosticDescriptors.BinderVarAlreadyDeclared, name);
            return new BoundErrorStmt();
        }

        SymbolType declared = InferTypeInTypedDecl(typeToken);

        if (declared == SymbolType.DiagnosticsError) {
            scopes.Peek().Add(name, CreateErrorLocal(name));
            return new BoundErrorStmt();
        }

        if (initBoundExpr.type == SymbolType.DiagnosticsError) {
            scopes.Peek().Add(name, CreateErrorLocal(name));
            return new BoundErrorStmt();
        }

        if (declared != initBoundExpr.type) {
            scopes.Peek().Add(name, CreateErrorLocal(name));
            ReportError(DiagnosticDescriptors.BinderDeclaredAndAssignedTypeMismatch);
            return new BoundErrorStmt();
        }


        int index = AllocateLocalIndex();
        LocalSymbol localSymbol = new LocalSymbol(name, declared, index);
        scopes.Peek().Add(varDeclarationStmt.name.Text, localSymbol);


        return new BoundVarDeclarationStmt(localSymbol, initBoundExpr);
    }



    BoundExpr BindExpr(Expr expr) {
        return expr switch {
            VarAssignmentExpr a => BindVarAssignmentExpr(a),
            NameExpr n => BindNameExpr(n),
            LiteralExpr l => BindLiteralExpr(l),
            UnaryExpr u => BindUnaryExpr(u),
            BinaryExpr b => BindBinaryExpr(b),
            _ => throw new Exception($"Unexpected expr: {expr.syntaxKind}"),
        };
    }

    BoundExpr BindVarAssignmentExpr(VarAssignmentExpr varAssignmentStmt) {
        var name = varAssignmentStmt.variable.Text;
        var local = lookUpLocal(name);
        SymbolType localType = local is null ? SymbolType.DiagnosticsError : local.symbolType;

        var assignmentExpr = BindExpr(varAssignmentStmt.assignmentExpr);

        if (local is null) {
            ReportError(DiagnosticDescriptors.BinderVariableNotDeclared, name);
            return new BoundErrorExpr();
        }

        if (assignmentExpr.type != localType && assignmentExpr.type != SymbolType.DiagnosticsError && localType != SymbolType.DiagnosticsError) {
            ReportError(DiagnosticDescriptors.BinderDeclaredAndAssignedTypeMismatch);
        }

        return new BoundVarAssignmentExpr(local, assignmentExpr, local.symbolType);
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

    BoundExpr BindUnaryExpr(UnaryExpr unaryExpr) {
        BoundExpr boundOperatedExpr = BindExpr(unaryExpr.operatedExpr);
        var boundUnaryOperator = BoundUnaryOperator.GetUnaryOperator(unaryExpr.Operator.TokenType, boundOperatedExpr.type);

        if (boundUnaryOperator is not null) {
            return new BoundUnaryExpr(boundOperatedExpr, boundUnaryOperator, boundUnaryOperator.resultType);
        }

        if (boundOperatedExpr.type == SymbolType.DiagnosticsError) {
            return new BoundErrorExpr();
        }

        ReportError(DiagnosticDescriptors.BinderBinaryTypeMismatch);
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

    int EnterLoop() {
        return loopDepth += 1;
    }

    int ExitLoop() {
        return loopDepth -= 1;
    }

    bool isInLoop() {
        return loopDepth > 0;
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
    LocalSymbol CreateErrorLocal(string name) {
        return new LocalSymbol(name, SymbolType.DiagnosticsError, -1);
    }
    void ReportError(DiagnosticDescriptor descriptor, params object[] args) {
        diagnostics.Report(descriptor, args);
    }
}

