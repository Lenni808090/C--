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

    TypeSymbol currentReturnType;
    int loopDepth;
    DiagnosticBag diagnostics;
    List<BoundFunctionDeclaration> functions;
    BoundFunctionDeclaration? mainFunc;
    bool sawInvalidMainFunction;
    int nextLocalIndex;
    Stmt[] stmtsToBind;

    private static readonly Dictionary<string, TypeSymbol> namedTypes =
      new()
    {
        { "int", BuiltInTypes.Int },
        { "bool", BuiltInTypes.Bool },
        { "char", BuiltInTypes.Char},
    };

    public Binder(CompilationUnit compilationUnit, CompilerContext compilerContext) {
        scopes = new();
        functions = new();
        functionsByName = new();
        functionDeclarationsToBind = new();
        diagnostics = compilerContext.diagnostics;
        stmtsToBind = compilationUnit.stmts;
        loopDepth = 0;
        currentReturnType = BuiltInTypes.Error;
        sawInvalidMainFunction = false;
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

        var returnType = BindTypeSyntax(functionDeclaration.returnType);

        List<TypeSymbol> argTypes = new();
        HashSet<string> seenNames = new();
        foreach (ParameterSyntax arg in functionDeclaration.@params) {
            var type = BindTypeSyntax(arg.type);
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

        if (mainFunc is null && !sawInvalidMainFunction) {
            ReportError(GetCompilationLocation(), DiagnosticDescriptors.BinderProgramNeedsEntryPoint);
        }

        if (mainFunc is null) {
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
            ValidateMainFunc(func, functionSymbol);
        }

        PopScope();
        return func;
    }

    void ValidateMainFunc(BoundFunctionDeclaration func, FunctionSymbol functionSymbol) {
        if (mainFunc is not null) {
            ReportError(func.location, DiagnosticDescriptors.BinderMainFunctionAlreadyDefined);
        }
        else {
            if (functionSymbol.argCount > 0) {
                ReportError(func.location, DiagnosticDescriptors.BinderMainFunctionCannotHaveParameters);
                sawInvalidMainFunction = true;
            }
            else {
                mainFunc = func;
            }
        }
    }

    void AddParams(FunctionDeclarationStmt functionDeclaration) {
        foreach (var param in functionDeclaration.@params) {
            string name = param.name.Text;

            TypeSymbol type = BindTypeSyntax(param.type);
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
        throw new Exception("unsupported statement in binder: " + stmt.syntaxKind);
    }

    BoundStmt BindExpressionStmt(ExpressionStmt expressionStmt) {
        return new BoundExpressionStmt(BindExpr(expressionStmt.Expression), expressionStmt.location);
    }

    BoundStmt BindReturnStmt(ReturnStmt returnStmt) {
        var boundReturnedExpr = BindExpr(returnStmt.returnExpr);
        if (IsValidType(boundReturnedExpr.type) && !currentReturnType.IsSameType(boundReturnedExpr.type)) {
            ReportError(returnStmt.location, DiagnosticDescriptors.BinderReturnTypeMismatch, currentReturnType, boundReturnedExpr.type);
        }
        return new BoundReturnStmt(boundReturnedExpr, returnStmt.location);
    }

    BoundStmt BindIfStmt(IfStmt ifStmt) {
        BoundExpr boundConditionExpr = BindExpr(ifStmt.condition);

        if (!boundConditionExpr.type.IsSameType(BuiltInTypes.Bool) && IsValidType(boundConditionExpr.type)) {
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

        if (!boundConditionExpr.type.IsSameType(BuiltInTypes.Bool) && IsValidType(boundConditionExpr.type)) {
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

        if (!condition.type.IsSameType(BuiltInTypes.Bool) && IsValidType(condition.type)) {
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

        TypeSyntax typeToken = varDeclarationStmt.type;


        BoundExpr initBoundExpr = BindExpr(varDeclarationStmt.declarementExpr);

        bool isAlreadyInScope = scopes.Peek().ContainsKey(name);
        if (isAlreadyInScope) {
            ReportError(varDeclarationStmt.location, DiagnosticDescriptors.BinderVarAlreadyDeclared, name);
            return new BoundErrorStmt(varDeclarationStmt.location);
        }

        BoundModifiers modifiers = BindModifiers(varDeclarationStmt.modifiers);


        TypeSymbol declared = BindTypeSyntax(typeToken);

        if (!IsValidType(declared)) {
            scopes.Peek().Add(name, CreateErrorLocal(name, modifiers));
            return new BoundErrorStmt(varDeclarationStmt.location);
        }

        if (!IsValidType(initBoundExpr.type)) {
            scopes.Peek().Add(name, CreateErrorLocal(name, modifiers));
            return new BoundErrorStmt(varDeclarationStmt.location);
        }

        if (!declared.IsSameType(initBoundExpr.type)) {
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
            AssignmentExpr a => BindAssignmentExpr(a),
            CallExpr c => BindCallExpr(c),
            ArrayCreationExpr a => BindArrayCreationExpr(a),
            IndexExpr i => BindIndexExpr(i),
            NameExpr n => BindNameExpr(n),
            LiteralExpr l => BindLiteralExpr(l),
            UnaryExpr u => BindUnaryExpr(u),
            BinaryExpr b => BindBinaryExpr(b),
            _ => BindUnexpectedExpr(expr),
        };
    }

    BoundExpr BindUnexpectedExpr(Expr expr) {
        throw new Exception("unsupported expression in binder: " + expr.syntaxKind);
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

            if (!expectedType.IsSameType(gotType)) {
                ReportError(callExpr.args[i].location, DiagnosticDescriptors.BinderCallArgumentTypeMismatch, name, i + 1, expectedType, gotType);
                hasError = true;
            }
        }

        if (hasError) {
            return new BoundErrorExpr(callExpr.location);
        }

        return new BoundCallExpr(args.ToArray(), functionSymbol, functionSymbol.returnType, callExpr.location);
    }

    BoundExpr BindArrayCreationExpr(ArrayCreationExpr arrayCreationExpr) {
        TypeSymbol type = BindTypeSyntax(arrayCreationExpr.typeSyntax);

        if (!IsValidType(type)) {
            return new BoundErrorExpr(arrayCreationExpr.location);
        }

        if (type is not ArraySymbolType) {
            ReportError(arrayCreationExpr.typeSyntax.location, DiagnosticDescriptors.BinderArrayCreationTypeMustBeArray);
            return new BoundErrorExpr(arrayCreationExpr.location);
        }

        BoundExpr length = BindExpr(arrayCreationExpr.length);

        if (!IsValidType(length.type)) {
            return new BoundErrorExpr(arrayCreationExpr.location);
        }

        if (!length.type.IsSameType(BuiltInTypes.Int)) {
            ReportError(arrayCreationExpr.length.location, DiagnosticDescriptors.BinderArrayLengthMustBeInt);
            return new BoundErrorExpr(arrayCreationExpr.location);
        }

        return new BoundArrayCreationExpr(length, type, arrayCreationExpr.location);
    }

    BoundExpr BindIndexExpr(IndexExpr indexExpr) {
        BoundExpr target = BindExpr(indexExpr.target);

        if (!IsValidType(target.type)) {
            return new BoundErrorExpr(indexExpr.location);
        }

        if (target.type is not ArraySymbolType) {
            ReportError(indexExpr.target.location, DiagnosticDescriptors.BinderIndexTargetMustBeArray);
            return new BoundErrorExpr(indexExpr.location);
        }

        BoundExpr index = BindExpr(indexExpr.index);

        if (!IsValidType(index.type)) {
            return new BoundErrorExpr(indexExpr.location);
        }

        if (!index.type.IsSameType(BuiltInTypes.Int)) {
            ReportError(indexExpr.index.location, DiagnosticDescriptors.BinderArrayIndexMustBeInt);
            return new BoundErrorExpr(indexExpr.location);
        }

        var type = ((ArraySymbolType)target.type).elementType;

        return new BoundIndexExpr(index, target, type, indexExpr.location);
    }

    BoundExpr BindAssignmentExpr(AssignmentExpr assignmentExpr) {
        if (assignmentExpr.target is NameExpr) {
            return BindVarAssignmentExpr(assignmentExpr);
        }

        if (assignmentExpr.target is IndexExpr) {
            return BindIndexAssignmentExpr(assignmentExpr);
        }

        ReportError(assignmentExpr.target.location, DiagnosticDescriptors.BinderAssignmentTargetMustBeAssignable);
        return new BoundErrorExpr(assignmentExpr.location);

    }

    BoundExpr BindVarAssignmentExpr(AssignmentExpr assignmentExpr) {

        if (assignmentExpr.target is not NameExpr nameExpr) {
            ReportError(assignmentExpr.location, DiagnosticDescriptors.BinderVariableNotDeclared, "<assignment target>");
            return new BoundErrorExpr(assignmentExpr.location);
        }

        var name = nameExpr.name.Text;
        var local = lookUpLocal(name);
        var assignedExpr = BindExpr(assignmentExpr.value);

        if (local is null) {
            ReportError(assignmentExpr.location, DiagnosticDescriptors.BinderVariableNotDeclared, name);
            return new BoundErrorExpr(assignmentExpr.location);
        }

        if (!local.modifiers.isMutable) {
            ReportError(assignmentExpr.location, DiagnosticDescriptors.BinderInmutableAssignment);
            return new BoundErrorExpr(assignmentExpr.location);
        }

        var assignmentOperatorType = assignmentExpr.assignmentOperator.TokenType;

        if (assignmentOperatorType == TokenType.Equals) {
            return BindSimpleVarAssignment(local, assignedExpr, assignmentExpr.location);
        }

        return BindCompoundVarAssignment(local, assignedExpr, assignmentOperatorType, assignmentExpr.location);
    }
    BoundExpr BindSimpleVarAssignment(LocalSymbol local, BoundExpr assignedExpr, SourceLocation location) {
        if (!IsValidType(assignedExpr.type) || !IsValidType(local.typeSymbol)) {
            return new BoundErrorExpr(location);
        }

        if (!assignedExpr.type.IsSameType(local.typeSymbol)) {
            ReportError(location, DiagnosticDescriptors.BinderDeclaredAndAssignedTypeMismatch);
            return new BoundErrorExpr(location);
        }

        return new BoundVarAssignmentExpr(local, assignedExpr, local.typeSymbol, location);
    }

    BoundExpr BindCompoundVarAssignment(LocalSymbol local, BoundExpr assignedExpr, TokenType assignmentOperatorType, SourceLocation location) {
        if (!IsValidType(assignedExpr.type) || !IsValidType(local.typeSymbol)) {
            return new BoundErrorExpr(location);
        }

        var boundOp = MapCompoundAssignmentToBinaryOperator(assignmentOperatorType, local.typeSymbol, assignedExpr.type);
        if (boundOp is null) {
            ReportError(location, DiagnosticDescriptors.BinderBinaryTypeMismatch);
            return new BoundErrorExpr(location);
        }

        var compoundExpr = new BoundBinaryExpr(new BoundNameExpr(local, location), assignedExpr, boundOp, boundOp.resultType, location);
        if (!compoundExpr.type.IsSameType(local.typeSymbol)) {
            ReportError(location, DiagnosticDescriptors.BinderDeclaredAndAssignedTypeMismatch);
            return new BoundErrorExpr(location);
        }

        return new BoundVarAssignmentExpr(local, compoundExpr, local.typeSymbol, location);
    }

    BoundExpr BindIndexAssignmentExpr(AssignmentExpr assignmentExpr) {
        if (assignmentExpr.target is not IndexExpr indexExpr) {
            ReportError(assignmentExpr.location, DiagnosticDescriptors.BinderVariableNotDeclared, "<assignment target>");
            return new BoundErrorExpr(assignmentExpr.location);
        }
        BoundExpr boundTarget = BindExpr(indexExpr.target);

        if (!IsValidType(boundTarget.type)) {
            return new BoundErrorExpr(assignmentExpr.location);
        }

        if (boundTarget.type is not ArraySymbolType arrayType) {
            ReportError(indexExpr.target.location, DiagnosticDescriptors.BinderIndexTargetMustBeArray);
            return new BoundErrorExpr(assignmentExpr.location);
        }

        BoundExpr index = BindExpr(indexExpr.index);

        if (!IsValidType(index.type)) {
            return new BoundErrorExpr(assignmentExpr.location);
        }

        if (!index.type.IsSameType(BuiltInTypes.Int)) {
            ReportError(indexExpr.index.location, DiagnosticDescriptors.BinderArrayIndexMustBeInt);
            return new BoundErrorExpr(assignmentExpr.location);
        }

        BoundExpr value = BindExpr(assignmentExpr.value);

        if (!IsValidType(value.type)) {
            return new BoundErrorExpr(assignmentExpr.location);
        }

        if (!value.type.IsSameType(arrayType.elementType)) {
            ReportError(assignmentExpr.location, DiagnosticDescriptors.BinderDeclaredAndAssignedTypeMismatch);
            return new BoundErrorExpr(assignmentExpr.location);
        }

        return new BoundIndexAssignmentExpr(boundTarget, index, value, arrayType.elementType, assignmentExpr.location);
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
        TypeSymbol type = InferLiteralType(literalToken);

        if (type.IsSameType(BuiltInTypes.Char) || type.IsSameType(BuiltInTypes.Int)) {
            if (!literalExpr.value.hasValue) {
                ReportError(literalExpr.location, DiagnosticDescriptors.BinderNumberLiteralMissingValue);
                return new BoundErrorExpr(literalExpr.location);
            }

            long v = literalExpr.value.Value;
            return new BoundLiteralExpr(v, type, literalExpr.location);
        }

        if (type.IsSameType(BuiltInTypes.Bool)) {
            long v = tokenType == TokenType.True ? 1 : 0;
            return new BoundLiteralExpr(v, type, literalExpr.location);
        }

        throw new Exception("unexpected literal type: " + type);
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

    bool IsValidType(TypeSymbol symbolType) {
        return !symbolType.IsSameType(BuiltInTypes.Error);
    }

    TypeSymbol BindTypeSyntax(TypeSyntax typeSyntax) {
        switch (typeSyntax) {
            case IdentifierTypeSyntax identifierType: {
                    if (TryResolveNamedType(identifierType, out TypeSymbol? typeSymbol)) {
                        return typeSymbol!;
                    }
                    break;
                }
            case ArrayTypeSyntax arrayTypeSyntax: {
                    TypeSymbol elementType = BindTypeSyntax(arrayTypeSyntax.elementType);
                    return new ArraySymbolType(elementType);
                }


            default: {
                    throw new Exception("Unkown type in infer type in typed decl");
                }

        }

        ReportError(typeSyntax.location, DiagnosticDescriptors.BinderUnknownTypeToken);
        return BuiltInTypes.Error;
    }

    bool TryResolveNamedType(IdentifierTypeSyntax identifierTypeSyntax, out TypeSymbol? typeSymbol) {
        string name = identifierTypeSyntax.identifier.Text;
        if (namedTypes.TryGetValue(name, out TypeSymbol? type)) {
            typeSymbol = type;
            return true;
        }
        else {
            typeSymbol = null;
            return false;
        }
    }


    TypeSymbol InferLiteralType(Token token) {
        TokenType tokenType = token.TokenType;
        switch (tokenType) {
            case TokenType.True:
            case TokenType.False: {
                    return BuiltInTypes.Bool;
                }
            case TokenType.Number: {
                    return BuiltInTypes.Int;
                }
            case TokenType.Char: {
                    return BuiltInTypes.Char;
                }
            default: {
                    throw new Exception("unkown type " + tokenType);
                }
        }

    }

    BoundBinaryOperator? MapCompoundAssignmentToBinaryOperator(TokenType tokenType, TypeSymbol leftSide, TypeSymbol rightSide) {
        return tokenType switch {
            TokenType.PlusEquals => BoundBinaryOperator.GetBinaryOperator(TokenType.Plus, leftSide, rightSide),
            TokenType.MinusEquals => BoundBinaryOperator.GetBinaryOperator(TokenType.Minus, leftSide, rightSide),
            TokenType.MultiplyEquals => BoundBinaryOperator.GetBinaryOperator(TokenType.Multiply, leftSide, rightSide),
            TokenType.DivideEquals => BoundBinaryOperator.GetBinaryOperator(TokenType.Divide, leftSide, rightSide),
            TokenType.ModulusEquals => BoundBinaryOperator.GetBinaryOperator(TokenType.Modulus, leftSide, rightSide),
            _ => throw new Exception("unkown compound assignment operator"),
        };
    }
    LocalSymbol CreateErrorLocal(string name, BoundModifiers modifiers) {
        return new LocalSymbol(name, BuiltInTypes.Error, modifiers, -1);
    }
    FunctionSymbol CreateErrorFunctionSymbol(string name) {
        return new FunctionSymbol(name, BuiltInTypes.Error, Array.Empty<TypeSymbol>());
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

