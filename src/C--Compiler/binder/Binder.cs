using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using CMinus.Compiler;
using CMinus.Compiler.Diagnostics;
using CMinus.Compiler.Syntax;
using Microsoft.VisualBasic;

namespace CMinus.Compiler.Binding;

class Binder {
    Stack<Dictionary<string, LocalSymbol>> scopes;

    Dictionary<string, FunctionSymbol> functionsByName;
    List<FunctionDeclarationStmt> functionDeclarationsToBind;

    SymbolType currentReturnType;
    int loopDepth;
    DiagnosticBag diagnostics;
    List<BoundFunctionDeclaration> functions;
    BoundFunctionDeclaration? mainFunc;
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
        functions = new();
        functionsByName = new();
        functionDeclarationsToBind = new();
        diagnostics = compilerContext.diagnostics;
        stmtsToBind = compilationUnit.stmts;
        loopDepth = 0;
        currentReturnType = SymbolType.DiagnosticsError;
    }

    public BoundCompiledUnit BindCompiledUnit() {
        CollectFunctions();
        return BindFunctions();
    }

    public void CollectFunctions() {
        foreach (Stmt stmt in stmtsToBind) {
            if (stmt is not FunctionDeclarationStmt func) {
                ReportError(stmt.location, DiagnosticDescriptors.BinderTopLevelStmtMustBeFunction);
                continue;
            }
            AddFunc(func);
        }
    }
    void AddFunc(FunctionDeclarationStmt functionDeclaration) {
        var name = functionDeclaration.functionName.Text;
        if (functionsByName.TryGetValue(name, out _)) {
            ReportError(functionDeclaration.location, DiagnosticDescriptors.BinderFunctionAlreadyDeclared, name);
            return;
        }

        var returnType = InferTypeInTypedDecl(((IdentifierTypeSyntax)functionDeclaration.returnType).identifier);

        List<SymbolType> argTypes = new();
        HashSet<string> seenNames = new();
        foreach (ParameterSyntax arg in functionDeclaration.@params) {
            var type = InferTypeInTypedDecl(((IdentifierTypeSyntax)arg.type).identifier);
            argTypes.Add(type);

            if (!seenNames.Add(arg.name.Text)) {
                ReportError(arg.location, DiagnosticDescriptors.BinderDuplicateParameterName, arg.name.Text);
            }
        }

        FunctionSymbol functionSymbol = new FunctionSymbol(name, returnType, argTypes.ToArray());
        functionsByName[name] = functionSymbol;
        functionDeclarationsToBind.Add(functionDeclaration);
    }

    public BoundCompiledUnit BindFunctions() {

        foreach (FunctionDeclarationStmt stmt in functionDeclarationsToBind) {
            var boundStmt = BindFunction(stmt);
            functions.Add(boundStmt);
        }

        if (mainFunc is null) {
            ReportError(GetCompilationLocation(), DiagnosticDescriptors.BinderProgramNeedsEntryPoint);
            mainFunc = CreateErrorMainFunction();
        }
        return new BoundCompiledUnit(mainFunc, functions.ToArray());
    }


    BoundFunctionDeclaration BindFunction(FunctionDeclarationStmt functionDeclaration) {
        ResetLocalIndex();
        PushScope();

        var name = functionDeclaration.functionName.Text;
        functionsByName.TryGetValue(name, out FunctionSymbol? functionSymbol);

        if (functionSymbol is null) {
            ReportError(functionDeclaration.location, DiagnosticDescriptors.BinderFunctionResolutionFailed, name);
            functionSymbol = CreateErrorFunctionSymbol(name);
        }

        currentReturnType = functionSymbol.returnType;

        AddParams(functionDeclaration);
        var body = (BoundBlockStmt)BindBlockStmt(functionDeclaration.functionBody);
        functionSymbol.localCount = nextLocalIndex;
        var func = new BoundFunctionDeclaration(functionSymbol, body, functionDeclaration.functionName.Location);

        if (name == "Main") {
            mainFunc = func;
        }

        PopScope();
        return func;
    }

    void AddParams(FunctionDeclarationStmt functionDeclaration) {
        foreach (var param in functionDeclaration.@params) {
            string name = param.name.Text;

            SymbolType type = InferTypeInTypedDecl(((IdentifierTypeSyntax)param.type).identifier);
            BoundModifiers modifiers = BindModifiers(param.modifiers);

            if (scopes.Peek().ContainsKey(name)) {
                ReportError(param.location, DiagnosticDescriptors.BinderDuplicateParameterName, name);
                continue;
            }

            int index = AllocateLocalIndex();
            LocalSymbol local = new LocalSymbol(name, type, modifiers, index);
            scopes.Peek().Add(name, local);
        }
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
            _ => BindUnexpectedStmt(stmt),
        };
    }

    BoundStmt BindUnexpectedStmt(Stmt stmt) {
        ReportError(stmt.location, DiagnosticDescriptors.BinderUnexpectedStatement, stmt.syntaxKind);
        return new BoundErrorStmt(stmt.location);
    }

    BoundStmt BindExpressionStmt(ExpressionStmt expressionStmt) {
        return new BoundExpressionStmt(BindExpr(expressionStmt.Expression), expressionStmt.location);
    }

    BoundStmt BindReturnStmt(ReturnStmt returnStmt) {
        var boundReturnedExpr = BindExpr(returnStmt.returnExpr);
        if (IsValidType(boundReturnedExpr.type) && currentReturnType != boundReturnedExpr.type) {
            ReportError(returnStmt.location, DiagnosticDescriptors.BinderReturnTypeMismatch, currentReturnType, boundReturnedExpr.type);
        }
        return new BoundReturnStmt(boundReturnedExpr, returnStmt.location);
    }

    BoundStmt BindIfStmt(IfStmt ifStmt) {
        BoundExpr boundConditionExpr = BindExpr(ifStmt.condition);

        if (boundConditionExpr.type != SymbolType.Bool && IsValidType(boundConditionExpr.type)) {
            ReportError(ifStmt.condition.location, DiagnosticDescriptors.BinderConditionMustBeBool);
        }

        BoundStmt thenStmt = BindStmt(ifStmt.thenStmt);

        if (ifStmt.elseStmt is null) {
            return new BoundIfStmt(boundConditionExpr, thenStmt, ifStmt.location);
        }

        BoundStmt elseStmt = BindStmt(ifStmt.elseStmt);

        return new BoundIfStmt(boundConditionExpr, thenStmt, ifStmt.location, elseStmt);
    }

    BoundStmt BindContinueStmt(ContinueStmt continueStmt) {
        if (!isInLoop()) {
            ReportError(continueStmt.location, DiagnosticDescriptors.BinderNotInLoopContinue);
        }
        return new BoundContinueStmt(continueStmt.location);
    }


    BoundStmt BindBreakStmt(BreakStmt breakStmt) {
        if (!isInLoop()) {
            ReportError(breakStmt.location, DiagnosticDescriptors.BinderNotInLoopBreak);
        }
        return new BoundBreakStmt(breakStmt.location);
    }
    BoundStmt BindWhileStmt(WhileStmt whileStmt) {

        BoundExpr boundConditionExpr = BindExpr(whileStmt.condition);

        if (boundConditionExpr.type != SymbolType.Bool && IsValidType(boundConditionExpr.type)) {
            ReportError(whileStmt.condition.location, DiagnosticDescriptors.BinderConditionMustBeBool);
        }

        EnterLoop();
        BoundStmt body = BindStmt(whileStmt.body);
        ExitLoop();

        return new BoundWhileStmt(boundConditionExpr, body, whileStmt.location);
    }


    BoundStmt BindForStmt(ForStmt forStmt) {
        PushScope();

        BoundStmt initializer;

        if (forStmt.declarationStmt is not null) {
            initializer = BindStmt(forStmt.declarationStmt);
        }
        else {
            var initializerExpr = BindExpr(forStmt.initializeExpr!);
            initializer = new BoundExpressionStmt(initializerExpr, forStmt.initializeExpr!.location);
        }

        var condition = BindExpr(forStmt.condition);

        if (condition.type != SymbolType.Bool && IsValidType(condition.type)) {
            ReportError(forStmt.condition.location, DiagnosticDescriptors.BinderConditionMustBeBool);
        }

        var iteration = BindExpr(forStmt.iteration);

        EnterLoop();
        BoundStmt body = BindStmt(forStmt.body);
        ExitLoop();

        PopScope();

        return new BoundForStmt(initializer, condition, iteration, body, forStmt.location);
    }

    BoundStmt BindBlockStmt(BlockStmt blockStmt) {
        PushScope();
        List<BoundStmt> boundStmts = new();

        foreach (Stmt stmt in blockStmt.stmts) {
            boundStmts.Add(BindStmt(stmt));
        }

        PopScope();
        return new BoundBlockStmt(boundStmts.ToArray(), blockStmt.location);
    }
    BoundStmt BindVarDeclarationStmt(VarDeclarationStmt varDeclarationStmt) {
        string name = varDeclarationStmt.name.Text;

        Token typeToken = ((IdentifierTypeSyntax)varDeclarationStmt.type).identifier;


        BoundExpr initBoundExpr = BindExpr(varDeclarationStmt.declarementExpr);

        bool isAlreadyInScope = scopes.Peek().ContainsKey(name);
        if (isAlreadyInScope) {
            ReportError(varDeclarationStmt.location, DiagnosticDescriptors.BinderVarAlreadyDeclared, name);
            return new BoundErrorStmt(varDeclarationStmt.location);
        }

        BoundModifiers modifiers = BindModifiers(varDeclarationStmt.modifiers);


        SymbolType declared = InferTypeInTypedDecl(typeToken);

        if (!IsValidType(declared)) {
            scopes.Peek().Add(name, CreateErrorLocal(name, modifiers));
            return new BoundErrorStmt(varDeclarationStmt.location);
        }

        if (!IsValidType(initBoundExpr.type)) {
            scopes.Peek().Add(name, CreateErrorLocal(name, modifiers));
            return new BoundErrorStmt(varDeclarationStmt.location);
        }

        if (declared != initBoundExpr.type) {
            scopes.Peek().Add(name, CreateErrorLocal(name, modifiers));
            ReportError(varDeclarationStmt.location, DiagnosticDescriptors.BinderDeclaredAndAssignedTypeMismatch);
            return new BoundErrorStmt(varDeclarationStmt.location);
        }


        int index = AllocateLocalIndex();
        LocalSymbol localSymbol = new LocalSymbol(name, declared, modifiers, index);
        scopes.Peek().Add(varDeclarationStmt.name.Text, localSymbol);


        return new BoundVarDeclarationStmt(localSymbol, initBoundExpr, varDeclarationStmt.location);
    }



    BoundExpr BindExpr(Expr expr) {
        return expr switch {
            VarAssignmentExpr a => BindVarAssignmentExpr(a),
            CallExpr c => BindCallExpr(c),
            NameExpr n => BindNameExpr(n),
            LiteralExpr l => BindLiteralExpr(l),
            UnaryExpr u => BindUnaryExpr(u),
            BinaryExpr b => BindBinaryExpr(b),
            _ => BindUnexpectedExpr(expr),
        };
    }

    BoundExpr BindUnexpectedExpr(Expr expr) {
        ReportError(expr.location, DiagnosticDescriptors.BinderUnexpectedExpression, expr.syntaxKind);
        return new BoundErrorExpr(expr.location);
    }

    BoundExpr BindVarAssignmentExpr(VarAssignmentExpr varAssignmentExpr) {
        var name = varAssignmentExpr.variable.Text;
        var local = lookUpLocal(name);
        var assignedExpr = BindExpr(varAssignmentExpr.assignmentExpr);

        if (local is null) {
            ReportError(varAssignmentExpr.location, DiagnosticDescriptors.BinderVariableNotDeclared, name);
            return new BoundErrorExpr(varAssignmentExpr.location);
        }

        if (!local.modifiers.isMutable) {
            ReportError(varAssignmentExpr.location, DiagnosticDescriptors.BinderInmutableAssignment);
            return new BoundErrorExpr(varAssignmentExpr.location);
        }

        var assignmentOperatorType = varAssignmentExpr.assignmentOperator.TokenType;

        if (assignmentOperatorType == TokenType.Equals) {
            return BindSimpleVarAssignment(local, assignedExpr, varAssignmentExpr.location);
        }

        return BindCompoundVarAssignment(local, assignedExpr, assignmentOperatorType, varAssignmentExpr.location);
    }


    BoundExpr BindCallExpr(CallExpr callExpr) {
        List<BoundExpr> args = new();
        foreach (Expr arg in callExpr.args) {
            args.Add(BindExpr(arg));
        }

        if (callExpr.calle is not NameExpr nameExpr) {
            ReportError(callExpr.calle.location, DiagnosticDescriptors.BinderCallTargetMustBeFunctionName);
            return new BoundErrorExpr(callExpr.location);
        }

        string name = nameExpr.name.Text;

        if (!functionsByName.TryGetValue(name, out FunctionSymbol? functionSymbol)) {
            ReportError(nameExpr.location, DiagnosticDescriptors.BinderFunctionNotDeclared, name);
            return new BoundErrorExpr(callExpr.location);
        }

        if (args.Count != functionSymbol.argCount) {
            ReportError(callExpr.location, DiagnosticDescriptors.BinderCallArgumentCountMismatch, name, functionSymbol.argCount, args.Count);
            return new BoundErrorExpr(callExpr.location);
        }

        bool hasError = false;
        for (int i = 0; i < args.Count; i++) {
            var expectedType = functionSymbol.argTypes[i];
            var gotType = args[i].type;

            if (!IsValidType(expectedType) || !IsValidType(gotType)) {
                hasError = true;
                continue;
            }

            if (expectedType != gotType) {
                ReportError(callExpr.args[i].location, DiagnosticDescriptors.BinderCallArgumentTypeMismatch, name, i + 1, expectedType, gotType);
                hasError = true;
            }
        }

        if (hasError) {
            return new BoundErrorExpr(callExpr.location);
        }

        return new BoundCallExpr(args.ToArray(), functionSymbol, functionSymbol.returnType, callExpr.location);
    }

    BoundExpr BindSimpleVarAssignment(LocalSymbol local, BoundExpr assignedExpr, SourceLocation location) {
        if (!IsValidType(assignedExpr.type) || !IsValidType(local.symbolType)) {
            return new BoundErrorExpr(location);
        }

        if (assignedExpr.type != local.symbolType) {
            ReportError(location, DiagnosticDescriptors.BinderDeclaredAndAssignedTypeMismatch);
            return new BoundErrorExpr(location);
        }

        return new BoundVarAssignmentExpr(local, assignedExpr, local.symbolType, location);
    }

    BoundExpr BindCompoundVarAssignment(LocalSymbol local, BoundExpr assignedExpr, TokenType assignmentOperatorType, SourceLocation location) {
        if (!IsValidType(assignedExpr.type) || !IsValidType(local.symbolType)) {
            return new BoundErrorExpr(location);
        }

        var boundOp = MapCompoundAssignmentToBinaryOperator(assignmentOperatorType, local.symbolType, assignedExpr.type);
        if (boundOp is null) {
            ReportError(location, DiagnosticDescriptors.BinderBinaryTypeMismatch);
            return new BoundErrorExpr(location);
        }

        var compoundExpr = new BoundBinaryExpr(new BoundNameExpr(local, location), assignedExpr, boundOp, boundOp.resultType, location);
        if (compoundExpr.type != local.symbolType) {
            ReportError(location, DiagnosticDescriptors.BinderDeclaredAndAssignedTypeMismatch);
            return new BoundErrorExpr(location);
        }

        return new BoundVarAssignmentExpr(local, compoundExpr, local.symbolType, location);
    }


    BoundExpr BindNameExpr(NameExpr nameExpr) {
        string name = nameExpr.name.Text;
        LocalSymbol? localSymbol = lookUpLocal(name);
        if (localSymbol is not null) {
            return new BoundNameExpr(localSymbol, nameExpr.location);
        }
        ReportError(nameExpr.location, DiagnosticDescriptors.BinderVariableNotDeclared, name);
        return new BoundErrorExpr(nameExpr.location);

    }

    BoundExpr BindLiteralExpr(LiteralExpr literalExpr) {
        Token literalToken = literalExpr.value;
        TokenType tokenType = literalToken.TokenType;
        SymbolType type = InferType(literalToken);

        if (type == SymbolType.Int) {
            if (!literalExpr.value.hasValue) {
                ReportError(literalExpr.location, DiagnosticDescriptors.BinderNumberLiteralMissingValue);
                return new BoundErrorExpr(literalExpr.location);
            }

            long v = literalExpr.value.Value;
            return new BoundLiteralExpr(v, type, literalExpr.location);
        }

        if (type == SymbolType.Bool) {
            long v = tokenType == TokenType.True ? 1 : 0;
            return new BoundLiteralExpr(v, type, literalExpr.location);
        }

        ReportError(literalExpr.location, DiagnosticDescriptors.BinderUnexpectedLiteralType, type);
        return new BoundErrorExpr(literalExpr.location);
    }

    BoundExpr BindUnaryExpr(UnaryExpr unaryExpr) {
        BoundExpr boundOperatedExpr = BindExpr(unaryExpr.operatedExpr);
        var boundUnaryOperator = BoundUnaryOperator.GetUnaryOperator(unaryExpr.Operator.TokenType, boundOperatedExpr.type);

        if (boundUnaryOperator is not null) {
            return new BoundUnaryExpr(boundOperatedExpr, boundUnaryOperator, boundUnaryOperator.resultType, unaryExpr.location);
        }

        if (!IsValidType(boundOperatedExpr.type)) {
            return new BoundErrorExpr(unaryExpr.location);
        }

        ReportError(unaryExpr.location, DiagnosticDescriptors.BinderBinaryTypeMismatch);
        return new BoundErrorExpr(unaryExpr.location);
    }

    BoundExpr BindBinaryExpr(BinaryExpr binaryExpr) {
        BoundExpr boundLeftExpr = BindExpr(binaryExpr.leftExpr);
        BoundExpr boundRightExpr = BindExpr(binaryExpr.rightExpr);

        var op = binaryExpr.Operator.TokenType;
        BoundBinaryOperator? boundBinaryOperator = BoundBinaryOperator.GetBinaryOperator(op, boundLeftExpr.type, boundRightExpr.type);
        if (boundBinaryOperator is not null) {
            return new BoundBinaryExpr(boundLeftExpr, boundRightExpr, boundBinaryOperator, boundBinaryOperator.resultType, binaryExpr.location);
        }

        if (!IsValidType(boundLeftExpr.type) || !IsValidType(boundRightExpr.type)) {
            return new BoundErrorExpr(binaryExpr.location);
        }

        ReportError(binaryExpr.location, DiagnosticDescriptors.BinderBinaryTypeMismatch);
        return new BoundErrorExpr(binaryExpr.location);

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
                        ReportError(token.Location, DiagnosticDescriptors.BinderDuplicateModifier);
                        return;
                    }
                    modified.isMutable = true;
                    break;
                }
            default:
                ReportError(token.Location, DiagnosticDescriptors.BinderUnkownModifier);
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

    void ResetLocalIndex() {
        nextLocalIndex = 0;
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

        ReportError(typeToken.Location, DiagnosticDescriptors.BinderUnknownTypeToken, typeToken.Text);
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
                    ReportError(token.Location, DiagnosticDescriptors.BinderUnknownTokenType, tokenType);
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
    FunctionSymbol CreateErrorFunctionSymbol(string name) {
        return new FunctionSymbol(name, SymbolType.DiagnosticsError, Array.Empty<SymbolType>());
    }
    BoundFunctionDeclaration CreateErrorMainFunction() {
        var functionSymbol = CreateErrorFunctionSymbol("Main");
        var body = new BoundBlockStmt(Array.Empty<BoundStmt>(), GetCompilationLocation());
        return new BoundFunctionDeclaration(functionSymbol, body, GetCompilationLocation());
    }
    SourceLocation GetCompilationLocation() {
        return stmtsToBind.Length > 0 ? stmtsToBind[0].location : SourceLocation.None;
    }
    void ReportError(SourceLocation location, DiagnosticDescriptor descriptor, params object[] args) {
        diagnostics.Report(location, descriptor, args);
    }
}

