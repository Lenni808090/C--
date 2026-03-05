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
        ReportError(DiagnosticDescriptors.ParserUnexpectedToken, message, unexpected.TokenType);

        if (ShouldConsumeUnexpectedToken(type, unexpected.TokenType)) {
            NextToken();
        }

        return new Token(string.Empty, type);
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
               tokenType == TokenType.OpenBrace ||
               tokenType == TokenType.CloseBrace ||
               tokenType == TokenType.Identifier ||
               tokenType == TokenType.Number ||
               tokenType == TokenType.True ||
               tokenType == TokenType.False ||
               tokenType == TokenType.OpenParentheses;
    }

    //int x = 100;
    bool matchesDeclarationStmt() {
        bool matches = true;
        if (Current.TokenType != TokenType.Identifier) {
            matches = false;
        }
        if (Peek(1).TokenType != TokenType.Identifier) {
            matches = false;
        }
        if (Peek(2).TokenType != TokenType.Equals) {
            matches = false;
        }
        return matches;
    }

    bool matchesAssignemntStmt() {
        bool matches = true;

        if (Current.TokenType != TokenType.Identifier) {
            matches = false;
        }

        if (Peek(1).TokenType != TokenType.Equals) {
            matches = false;
        }

        return matches;
    }
    public CompilationUnit ParseUnit() {
        List<Stmt> stmts = new();

        while (Current.TokenType != TokenType.EoF) {
            stmts.Add(ParseStmt());
        }

        return new CompilationUnit(stmts.ToArray());
    }


    Stmt ParseStmt() {

        if (matchesDeclarationStmt()) {
            return ParseDeclarationStmt();
        }
        else if (matchesAssignemntStmt()) {
            return ParseAssignemntStmt();
        }

        switch (Current.TokenType) {
            case TokenType.Return: {
                    return ParseReturmStmt();
                }
            case TokenType.If: {
                    return ParseIfStmt();
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

    Stmt ParseDeclarationStmt() {
        TypeSyntax type = ParseType();

        Token identifier = NextToken();

        //equals;
        NextToken();

        Expr assignedExpr = ParseExpr();

        Expect(TokenType.Semicolon, "Missing ';' after variable declaration");

        return new VarDeclarationStmt(type, identifier, assignedExpr);
    }

    Stmt ParseAssignemntStmt() {
        Token identifier = NextToken();
        //equals;
        NextToken();

        Expr assignedExpr = ParseExpr();

        Expect(TokenType.Semicolon, "Missing ';' after variable assignment");

        return new VarAssignmentStmt(identifier, assignedExpr);
    }

    Stmt ParseBlockStmt() {
        NextToken();

        List<Stmt> body = new();
        while (Current.TokenType != TokenType.CloseBrace && Current.TokenType != TokenType.EoF) {
            body.Add(ParseStmt());
        }

        if (Current.TokenType == TokenType.CloseBrace) {
            NextToken();
        }
        else {
            ReportError(DiagnosticDescriptors.ParserMissingClosingBrace);
        }

        return new BlockStmt(body.ToArray());
    }

    Stmt ParseContinueStmt() {
        NextToken();
        Expect(TokenType.Semicolon, "Missing ';' after continue");
        return new ContinueStmt();
    }
    Stmt ParseBreakStmt() {
        NextToken();
        Expect(TokenType.Semicolon, "Missing ';' after break");
        return new BreakStmt();
    }
    Stmt ParseReturmStmt() {
        NextToken();
        Expr returnExpr = ParseExpr();
        Expect(TokenType.Semicolon, "Missing ';' after return");
        return new ReturnStmt(returnExpr);
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
                    ReportError(DiagnosticDescriptors.ParserExpectedPrimaryExpression, token.TokenType, token.Text);
                    if (Current.TokenType != TokenType.EoF) {
                        NextToken();
                    }
                    return new LiteralExpr(new Token("0", TokenType.Number, 0));
                }
        }
    }

    void ReportError(DiagnosticDescriptor descriptor, params object[] args) {
        diagnostics.Report(descriptor, args);
    }

}
