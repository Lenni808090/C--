using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using CMinus.Compiler;
using CMinus.Compiler.Diagnostics;
using CMinus.Compiler.Syntax;

namespace CMinus.Compiler.Parsing;

class Parser {
    private readonly Token[] tokens;
    private int position;

    DiagnosticBag diagnostics;
    public Parser(Token[] tokens, CompilerContext context) {
        this.tokens = tokens;
        diagnostics = context.diagnostics;
    }

    Token Current => Peek(0);

    Token Peek(int offset) {
        int index = position + offset;
        if (index >= tokens.Length) {
            return tokens[tokens.Length - 1];
        }

        return tokens[index];
    }

    Token NextToken() {
        Token current = Current;
        position++;
        return current;
    }

    Token Expect(TokenType type, string message) {
        if (Current.TokenType == type) {
            return NextToken();
        }

        Token unexpected = Current;
        ReportError(unexpected, DiagnosticDescriptors.ParserUnexpectedToken, message, unexpected.TokenType);

        if (ShouldConsumeUnexpectedToken(type, unexpected.TokenType)) {
            NextToken();
        }

        return new Token(string.Empty, type, unexpected.Location);
    }

    bool Match(TokenType type) {
        if (Current.TokenType == type) {
            NextToken();
            return true;
        }

        return false;
    }

    bool ShouldConsumeUnexpectedToken(TokenType expected, TokenType actual) {
        if (actual == TokenType.EoF) {
            return false;
        }

        if (expected == TokenType.Semicolon && IsStmtBoundaryToken(actual)) {
            return false;
        }

        if (expected == TokenType.CloseParentheses &&
            (actual == TokenType.OpenBrace || actual == TokenType.Semicolon || actual == TokenType.CloseBrace)) {
            return false;
        }

        return true;
    }

    bool IsStmtBoundaryToken(TokenType tokenType) {
        return tokenType == TokenType.Return ||
               tokenType == TokenType.If ||
               tokenType == TokenType.While ||
               tokenType == TokenType.For ||
               tokenType == TokenType.OpenBrace ||
               tokenType == TokenType.CloseBrace ||
               tokenType == TokenType.Identifier ||
               tokenType == TokenType.Number ||
               tokenType == TokenType.True ||
               tokenType == TokenType.False ||
               tokenType == TokenType.OpenParentheses ||
               tokenType == TokenType.Mut;
    }

    bool IsModifierToken(TokenType tokenType) {
        return tokenType == TokenType.Mut;
    }

    //int x = 100;
    bool MatchesDeclarationStmt(int offset = 0) {
        if (Peek(offset).TokenType != TokenType.Identifier) {
            return false;
        }

        if (Peek(offset + 1).TokenType != TokenType.Colon) {
            return false;
        }

        return true;
    }



    bool IsDeclarationStmt() {
        int offset = 0;

        while (IsModifierToken(Peek(offset).TokenType)) {
            offset++;
        }

        bool isDecl = MatchesDeclarationStmt(offset);
        return isDecl;
    }

    bool matchesAssignemntExpr() {
        bool matches = true;

        if (Current.TokenType != TokenType.Identifier) {
            matches = false;
        }

        if (!IsAssignmentOperator(Peek(1).TokenType)) {
            matches = false;
        }

        return matches;
    }

    bool IsAssignmentOperator(TokenType tokenType) {
        return tokenType == TokenType.Equals
               || tokenType == TokenType.PlusEquals
               || tokenType == TokenType.MinusEquals
               || tokenType == TokenType.MultiplyEquals
               || tokenType == TokenType.DivideEquals
               || tokenType == TokenType.ModulusEquals;
    }
    public CompilationUnit ParseUnit() {
        List<Stmt> stmts = new();

        while (Current.TokenType != TokenType.EoF) {
            stmts.Add(ParseStmt());
        }

        return new CompilationUnit(stmts.ToArray());
    }


    Stmt ParseStmt() {
        if (IsModifierToken(Current.TokenType) && !IsDeclarationStmt()) {
            ReportError(Current, DiagnosticDescriptors.ParserDeclarationExpectedAfterModifiers);
            ParseModifiers();
        }

        if (IsDeclarationStmt()) {
            return ParseDeclarationStmt();
        }

        switch (Current.TokenType) {
            case TokenType.Return: {
                    return ParseReturmStmt();
                }
            case TokenType.Meth: {
                    return ParseFunctionDeclarationStmt();
                }
            case TokenType.If: {
                    return ParseIfStmt();
                }
            case TokenType.For: {
                    return ParseForStmt();
                }
            case TokenType.While: {
                    return ParseWhileStmt();
                }
            case TokenType.OpenBrace: {
                    return ParseBlockStmt();
                }
            case TokenType.Continue: {
                    return ParseContinueStmt();
                }
            case TokenType.Break: {
                    return ParseBreakStmt();
                }
            default: {
                    Expr expr = ParseExpr();
                    Expect(TokenType.Semicolon, "Missing ';' after expression");
                    return new ExpressionStmt(expr);
                }
        }
    }

    Token[] ParseModifiers() {
        List<Token> modifiers = new();
        while (IsModifierToken(Current.TokenType)) {
            modifiers.Add(NextToken());
        }
        return modifiers.ToArray();
    }

    Stmt ParseDeclarationStmt() {
        var modifiers = ParseModifiers();
        var decl = ParseVarDeclarationCore(modifiers);
        Expect(TokenType.Semicolon, "missing ';' after declaration");
        return decl;
    }


    VarDeclarationStmt ParseVarDeclarationCore(Token[] modifiers) {
        Token identifier = NextToken();
        NextToken();
        var type = ParseType();
        Expect(TokenType.Equals, "Equals expected after type declaration in var declaration");
        Expr init = ParseExpr();
        return new VarDeclarationStmt(modifiers, type, identifier, init);
    }

    Stmt ParseBlockStmt() {
        Token openBrace = Expect(TokenType.OpenBrace, "Open Brace Expected in Block Stmt");

        List<Stmt> body = new();
        while (Current.TokenType != TokenType.CloseBrace && Current.TokenType != TokenType.EoF) {
            body.Add(ParseStmt());
        }

        if (Current.TokenType == TokenType.CloseBrace) {
            NextToken();
        }
        else {
            ReportError(openBrace, DiagnosticDescriptors.ParserMissingClosingBrace);
        }

        return new BlockStmt(openBrace, body.ToArray());
    }

    Stmt ParseContinueStmt() {
        Token keyword = NextToken();
        Expect(TokenType.Semicolon, "Missing ';' after continue");
        return new ContinueStmt(keyword);
    }
    Stmt ParseBreakStmt() {
        Token keyword = NextToken();
        Expect(TokenType.Semicolon, "Missing ';' after break");
        return new BreakStmt(keyword);
    }
    Stmt ParseReturmStmt() {
        Token keyword = NextToken();
        Expr returnExpr = ParseExpr();
        Expect(TokenType.Semicolon, "Missing ';' after return");
        return new ReturnStmt(keyword, returnExpr);
    }

    Stmt ParseFunctionDeclarationStmt() {
        NextToken();

        var functionName = Expect(TokenType.Identifier, "Function Name after meth keyword expected");

        Expect(TokenType.OpenParentheses, "Expected Open Parentheses after Funciiton Name");

        List<ParameterSyntax> parameters = new();
        while (Current.TokenType != TokenType.CloseParentheses && Current.TokenType != TokenType.EoF) {
            var paramName = Expect(TokenType.Identifier, "parameter identifier expected inside parentheses");

            Expect(TokenType.Colon, "Colon expected after Param Name for Type Definition");

            var parameterType = Expect(TokenType.Identifier, "Type Expexted after Colon when declaring params");
            parameters.Add(new ParameterSyntax(paramName, new IdentifierTypeSyntax(parameterType), new Token[0]));
            if (Current.TokenType == TokenType.Comma) {
                NextToken();
            }
        }

        Expect(TokenType.CloseParentheses, "Expected Close Parentheses after Params");

        Expect(TokenType.Arrow, "Arrow Expected After Params for Function Return Type");

        var returnType = Expect(TokenType.Identifier, "Function Return Type Expected After Arrow");

        var body = (BlockStmt)ParseBlockStmt();

        return new FunctionDeclarationStmt(functionName, parameters.ToArray(), new IdentifierTypeSyntax(returnType), body);
    }

    Stmt ParseIfStmt() {
        NextToken();

        Expect(TokenType.OpenParentheses, "opening ( expected after if");
        Expr condition = ParseExpr();
        Expect(TokenType.CloseParentheses, "closing ) expected after condition");

        Stmt thenStmt = ParseStmt();

        if (Current.TokenType != TokenType.Else) {
            return new IfStmt(condition, thenStmt);
        }

        NextToken();

        Stmt elseStmt = ParseStmt();

        return new IfStmt(condition, thenStmt, elseStmt);
    }

    Stmt ParseForStmt() {
        NextToken();

        Expect(TokenType.OpenParentheses, "opening ( expected after for");
        Expr? initializerExpr = null;
        VarDeclarationStmt? declarationStmt = null;
        if (IsDeclarationStmt()) {
            var modifiers = ParseModifiers();
            declarationStmt = ParseVarDeclarationCore(modifiers);
        }
        else if (matchesAssignemntExpr()) {
            initializerExpr = ParseAssignemntExpr();
        }
        else {
            ReportError(Current, DiagnosticDescriptors.ParserForLoopNeedsAssignmentOrDeclaration);
        }
        Expect(TokenType.Semicolon, "Semnicolon expected after Decl or Assign in For");

        Expr condition = ParseExpr();
        Expect(TokenType.Semicolon, "Semnicolon expected after Condition in For");

        Expr iteration = ParseExpr();
        Expect(TokenType.CloseParentheses, "CLosing Parentheses expected after iteration in For");

        var body = ParseStmt();

        return new ForStmt(declarationStmt, initializerExpr, condition, iteration, body);
    }

    Stmt ParseWhileStmt() {
        NextToken();

        Expect(TokenType.OpenParentheses, "opening ( expected after while");
        Expr condition = ParseExpr();
        Expect(TokenType.CloseParentheses, "closing ) expected after condition");

        var body = ParseStmt();

        return new WhileStmt(condition, body);
    }

    Expr ParseObjectCreationExpr() {
        NextToken();

        TypeSyntax arrayTypeSyntax = new ArrayTypeSyntax(ParseIdentifierType());
        Expect(TokenType.OpenBracket, "opening bracket expected after array type");
        Expr length = ParseExpr();
        Expect(TokenType.CloseBracket, "closing bracket expected after length in array creation");

        while (Current.TokenType == TokenType.OpenBracket) {
            NextToken();

            if (Current.TokenType == TokenType.CloseBracket) {
                NextToken();
                arrayTypeSyntax = new ArrayTypeSyntax(arrayTypeSyntax);
                continue;
            }

            ReportError(Current, DiagnosticDescriptors.ParserJaggedArrayCreationAdditionalDimensionsMustBeUnsized);
            ParseExpr();
            Expect(TokenType.CloseBracket, "closing bracket expected after length in array creation");
            arrayTypeSyntax = new ArrayTypeSyntax(arrayTypeSyntax);
        }

        return new ArrayCreationExpr(arrayTypeSyntax, length);
    }

    TypeSyntax ParseType() {
        TypeSyntax type = ParseIdentifierType();

        while (Current.TokenType == TokenType.OpenBracket) {
            NextToken();
            Expect(TokenType.CloseBracket, "closing bracket expected after '[' in type");
            type = new ArrayTypeSyntax(type);
        }

        return type;
    }

    TypeSyntax ParseIdentifierType() {
        Token type = Expect(TokenType.Identifier, "type identifier expected");
        return new IdentifierTypeSyntax(type);
    }
    Expr ParseCallExpr(Expr callee) {
        Expect(TokenType.OpenParentheses, "Expected '(' after callable expression");

        List<Expr> args = new();

        while (Current.TokenType != TokenType.CloseParentheses && Current.TokenType != TokenType.EoF) {
            args.Add(ParseExpr());

            if (Current.TokenType == TokenType.Comma) {
                NextToken();
            }
        }

        Expect(TokenType.CloseParentheses, "Expected ')' after arguments");
        return new CallExpr(callee, args.ToArray());
    }

    Expr ParseIndexExpr(Expr target) {

        Expect(TokenType.OpenBracket, "Expected '[' after indexed expression");
        Expr index = ParseExpr();
        Expect(TokenType.CloseBracket, "Expected ']' after indexed expression");

        return new IndexExpr(target, index);
    }

    Expr ParseExpr() {
        return ParseAssignemntExpr();
    }

    Expr ParseAssignemntExpr() {
        if (matchesAssignemntExpr()) {
            Token identifier = NextToken();
            var assignOP = NextToken();
            Expr assignedExpr = ParseExpr();
            return new VarAssignmentExpr(identifier, assignOP, assignedExpr);
        }
        return ParseLogicalOrExpr();
    }

    Expr ParseLogicalOrExpr() {
        Expr left = ParseLogicalAndExpr();

        while (Current.TokenType == TokenType.Or) {
            Token op = NextToken();
            Expr right = ParseLogicalAndExpr();
            left = new BinaryExpr(left, op, right);
        }

        return left;
    }

    Expr ParseLogicalAndExpr() {
        Expr left = ParseEqualityExpr();

        while (Current.TokenType == TokenType.And) {
            Token op = NextToken();
            Expr right = ParseEqualityExpr();
            left = new BinaryExpr(left, op, right);
        }

        return left;
    }


    Expr ParseEqualityExpr() {
        Expr left = ParseRelationalExpr();

        while (Current.TokenType == TokenType.EqualsEquals || Current.TokenType == TokenType.NotEquals) {
            Token op = NextToken();
            Expr right = ParseRelationalExpr();
            left = new BinaryExpr(left, op, right);
        }

        return left;
    }

    Expr ParseRelationalExpr() {
        Expr left = ParseAdditiveExpr();
        while (Current.TokenType == TokenType.MoreThen ||
            Current.TokenType == TokenType.MoreThenEquals ||
            Current.TokenType == TokenType.LessThen ||
            Current.TokenType == TokenType.LessThenEquals
        ) {
            Token op = NextToken();
            Expr right = ParseAdditiveExpr();
            left = new BinaryExpr(left, op, right);
        }

        return left;
    }
    Expr ParseAdditiveExpr() {
        Expr left = ParseMultiplyExpr();

        while (Current.TokenType == TokenType.Plus || Current.TokenType == TokenType.Minus) {
            Token op = NextToken();
            Expr right = ParseMultiplyExpr();
            left = new BinaryExpr(left, op, right);
        }

        return left;
    }

    Expr ParseMultiplyExpr() {
        Expr left = ParseUnaryExpr();

        while (Current.TokenType == TokenType.Multiply || Current.TokenType == TokenType.Divide || Current.TokenType == TokenType.Modulus) {
            Token op = NextToken();
            Expr right = ParseUnaryExpr();
            left = new BinaryExpr(left, op, right);
        }

        return left;

    }
    Expr ParseUnaryExpr() {
        if (Current.TokenType == TokenType.Bang || Current.TokenType == TokenType.Minus) {
            var Operator = NextToken();
            var operatedExpr = ParseUnaryExpr();
            return new UnaryExpr(Operator, operatedExpr);
        }

        return ParsePostfix();
    }

    Expr ParsePostfix() {
        Expr expr = ParsePrimary();

        while (Current.TokenType == TokenType.OpenParentheses || Current.TokenType == TokenType.OpenBracket) {
            if (Current.TokenType == TokenType.OpenParentheses) {
                expr = ParseCallExpr(expr);
            }
            else {
                expr = ParseIndexExpr(expr);
            }
        }

        return expr;
    }

    Expr ParsePrimary() {
        Token token = Current;
        switch (token.TokenType) {
            case TokenType.Char:
            case TokenType.Number:
            case TokenType.True:
            case TokenType.False: {
                    NextToken();
                    return new LiteralExpr(token);
                }
            case TokenType.New: {
                    var objCreationExpr = ParseObjectCreationExpr();
                    return objCreationExpr;
                }
            case TokenType.Identifier: {
                    NextToken();
                    return new NameExpr(token);
                }
            case TokenType.OpenParentheses: {
                    NextToken();
                    Expr expr = ParseExpr();
                    Expect(TokenType.CloseParentheses, "Closing ')' expected after '('");
                    return expr;
                }
            default: {
                    ReportError(token, DiagnosticDescriptors.ParserExpectedPrimaryExpression, token.TokenType, token.Text);
                    if (Current.TokenType != TokenType.EoF) {
                        NextToken();
                    }
                    return new LiteralExpr(new Token("0", TokenType.Number, token.Location, 0));
                }
        }
    }

    void ReportError(Token token, DiagnosticDescriptor descriptor, params object[] args) {
        diagnostics.Report(token, descriptor, args);
    }

}
