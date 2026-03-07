using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
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
    private static readonly Dictionary<string, SymbolType> standardTypes =
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
            ForStmt f => BindForStmt(f),
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

        if (boundConditionExpr.type != SymbolType.Bool && IsValidType(boundConditionExpr.type)) {
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

        if (boundConditionExpr.type != SymbolType.Bool && IsValidType(boundConditionExpr.type)) {
            ReportError(DiagnosticDescriptors.BinderConditionMustBeBool);
        }

        EnterLoop();
        BoundStmt body = BindStmt(whileStmt.body);
        ExitLoop();

        return new BoundWhileStmt(boundConditionExpr, body);
    }


    BoundStmt BindForStmt(ForStmt forStmt) {
        PushScope();

        BoundStmt initializer;

        if (forStmt.declarationStmt is not null) {
            initializer = BindStmt(forStmt.declarationStmt);
        }
        else {
            var initializerExpr = BindExpr(forStmt.initializeExpr!);
            initializer = new BoundExpressionStmt(initializerExpr);
        }

        var condition = BindExpr(forStmt.condition);

        if (condition.type != SymbolType.Bool && IsValidType(condition.type)) {
            ReportError(DiagnosticDescriptors.BinderConditionMustBeBool);
        }

        var iteration = BindExpr(forStmt.iteration);

        EnterLoop();
        BoundStmt body = BindStmt(forStmt.body);
        ExitLoop();

        PopScope();

        return new BoundForStmt(initializer, condition, iteration, body);
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

        BoundModifiers modifiers = BindModifiers(varDeclarationStmt.modifiers);


        SymbolType declared = InferTypeInTypedDecl(typeToken);

        if (!IsValidType(declared)) {
            scopes.Peek().Add(name, CreateErrorLocal(name, modifiers));
            return new BoundErrorStmt();
        }

        if (!IsValidType(initBoundExpr.type)) {
            scopes.Peek().Add(name, CreateErrorLocal(name, modifiers));
            return new BoundErrorStmt();
        }

        if (declared != initBoundExpr.type) {
            scopes.Peek().Add(name, CreateErrorLocal(name, modifiers));
            ReportError(DiagnosticDescriptors.BinderDeclaredAndAssignedTypeMismatch);
            return new BoundErrorStmt();
        }


        int index = AllocateLocalIndex();
        LocalSymbol localSymbol = new LocalSymbol(name, declared, modifiers, index);
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

    BoundExpr BindVarAssignmentExpr(VarAssignmentExpr varAssignmentExpr) {
        var name = varAssignmentExpr.variable.Text;
        var local = lookUpLocal(name);
        var assignedExpr = BindExpr(varAssignmentExpr.assignmentExpr);

        if (local is null) {
            ReportError(DiagnosticDescriptors.BinderVariableNotDeclared, name);
            return new BoundErrorExpr();
        }

        if (!local.modifiers.isMutable) {
            ReportError(DiagnosticDescriptors.BinderInmutableAssignment);
            return new BoundErrorExpr();
        }

        var assignmentOperatorType = varAssignmentExpr.assignmentOperator.TokenType;

        if (assignmentOperatorType == TokenType.Equals) {
            return BindSimpleVarAssignment(local, assignedExpr);
        }

        return BindCompoundVarAssignment(local, assignedExpr, assignmentOperatorType);
    }

    BoundExpr BindSimpleVarAssignment(LocalSymbol local, BoundExpr assignedExpr) {
        if (!IsValidType(assignedExpr.type) || !IsValidType(local.symbolType)) {
            return new BoundErrorExpr();
        }

        if (assignedExpr.type != local.symbolType) {
            ReportError(DiagnosticDescriptors.BinderDeclaredAndAssignedTypeMismatch);
            return new BoundErrorExpr();
        }

        return new BoundVarAssignmentExpr(local, assignedExpr, local.symbolType);
    }

    BoundExpr BindCompoundVarAssignment(LocalSymbol local, BoundExpr assignedExpr, TokenType assignmentOperatorType) {
        if (!IsValidType(assignedExpr.type) || !IsValidType(local.symbolType)) {
            return new BoundErrorExpr();
        }

        var boundOp = MapCompoundAssignmentToBinaryOperator(assignmentOperatorType, local.symbolType, assignedExpr.type);
        if (boundOp is null) {
            ReportError(DiagnosticDescriptors.BinderBinaryTypeMismatch);
            return new BoundErrorExpr();
        }

        var compoundExpr = new BoundBinaryExpr(new BoundNameExpr(local), assignedExpr, boundOp, boundOp.resultType);
        if (compoundExpr.type != local.symbolType) {
            ReportError(DiagnosticDescriptors.BinderDeclaredAndAssignedTypeMismatch);
            return new BoundErrorExpr();
        }

        return new BoundVarAssignmentExpr(local, compoundExpr, local.symbolType);
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

        if (!IsValidType(boundOperatedExpr.type)) {
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

        if (!IsValidType(boundLeftExpr.type) || !IsValidType(boundRightExpr.type)) {
            return new BoundErrorExpr();
        }

        ReportError(DiagnosticDescriptors.BinderBinaryTypeMismatch);
        return new BoundErrorExpr();

    }


    BoundModifiers BindModifiers(Token[] modifiers) {
        BoundModifiers modified = new();

        foreach (Token token in modifiers) {
            BindModifier(token, modified);
        }

        return modified;
    }

    void BindModifier(Token token, BoundModifiers modified) {
        TokenType tkType = token.TokenType;

        switch (tkType) {
            case TokenType.Mut: {
                    if (modified.isMutable) {
                        ReportError(DiagnosticDescriptors.BinderDuplicateModifier);
                        return;
                    }
                    modified.isMutable = true;
                    break;
                }
            default:
                ReportError(DiagnosticDescriptors.BinderUnkownModifier);
                break;
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

    bool IsValidType(SymbolType symbolType) {
        return symbolType != SymbolType.DiagnosticsError;
    }

    SymbolType InferTypeInTypedDecl(Token typeToken) {
        if (standardTypes.TryGetValue(typeToken.Text, out SymbolType type)) {
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

    BoundBinaryOperator? MapCompoundAssignmentToBinaryOperator(TokenType tokenType, SymbolType leftSide, SymbolType rightSide) {
        return tokenType switch {
            TokenType.PlusEquals => BoundBinaryOperator.GetBinaryOperator(TokenType.Plus, leftSide, rightSide),
            TokenType.MinusEquals => BoundBinaryOperator.GetBinaryOperator(TokenType.Minus, leftSide, rightSide),
            TokenType.MultiplyEquals => BoundBinaryOperator.GetBinaryOperator(TokenType.Multiply, leftSide, rightSide),
            TokenType.DivideEquals => BoundBinaryOperator.GetBinaryOperator(TokenType.Divide, leftSide, rightSide),
            _ => null,
        };
    }
    LocalSymbol CreateErrorLocal(string name, BoundModifiers modifiers) {
        return new LocalSymbol(name, SymbolType.DiagnosticsError, modifiers, -1);
    }
    void ReportError(DiagnosticDescriptor descriptor, params object[] args) {
        diagnostics.Report(descriptor, args);
    }
}

