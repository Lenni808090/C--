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
    bool matchesDeclarationStmt(int offset = 0) {
        bool matches = true;
        if (Peek(offset).TokenType != TokenType.Identifier) {
            matches = false;
        }
        if (Peek(offset + 1).TokenType != TokenType.Identifier) {
            matches = false;
        }
        if (Peek(offset + 2).TokenType != TokenType.Equals) {
            matches = false;
        }
        return matches;
    }

    bool IsDeclarationStmt() {
        int offset = 0;

        while (IsModifierToken(Peek(offset).TokenType)) {
            offset++;
        }

        bool isDecl = matchesDeclarationStmt(offset);
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
               || tokenType == TokenType.DivideEquals;
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
        var type = ParseType();
        Token identifier = NextToken();
        NextToken();
        Expr init = ParseExpr();
        return new VarDeclarationStmt(modifiers, type, identifier, init);
    }

    Stmt ParseBlockStmt() {
        Token openBrace = NextToken();

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

    TypeSyntax ParseType() {
        Token typeToken = Current;
        NextToken();
        return new IdentifierTypeSyntax(typeToken);
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
        while (Current.TokenType == TokenType.Multiply || Current.TokenType == TokenType.Divide) {
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

        return ParsePrimary();
    }


    Expr ParsePrimary() {
        Token token = Current;
        switch (token.TokenType) {
            case TokenType.Number:
            case TokenType.True:
            case TokenType.False: {
                    NextToken();
                    return new LiteralExpr(token);
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
