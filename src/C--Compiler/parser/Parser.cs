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

        ReportError(message + " got " + Current.TokenType);
        if (Current.TokenType != TokenType.EoF) {
            NextToken();
        }
        return new Token(string.Empty, type, new TextSpan(Current.TextSpan.Start, 0));
    }

    bool Match(TokenType type) {
        if (Current.TokenType == type) {
            NextToken();
            return true;
        }

        return false;
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

    public CompilationUnit ParseUnit() {
        List<Stmt> stmts = new();

        while (Current.TokenType != TokenType.EoF) {
            stmts.Add(ParseStmt());
        }

        return new CompilationUnit(stmts.ToArray());
    }


    Stmt ParseStmt() {

        if (matchesDeclarationStmt()) {
            TypeSyntax type = ParseType();
            Token identifier = Expect(TokenType.Identifier, "After type declaration an identifier is expected");
            Expect(TokenType.Equals, "After identifier '=' expected in var declaration");
            Expr assignedExpr = ParseExpr();
            Expect(TokenType.Semicolon, "Missing ';' after variable declaration");
            return new VarDeclarationStmt(type, identifier, assignedExpr);
        }

        switch (Current.TokenType) {
            case TokenType.Return: {
                    return ParseReturmStmt();
                }
            case TokenType.If: {
                    return ParseIfStmt();
                }
            case TokenType.OpenBrace: {
                    return ParseBlockStmt();
                }
            default: {
                    Expr expr = ParseExpr();
                    Expect(TokenType.Semicolon, "Missing ';' after expression");
                    return new ExpressionStmt(expr);
                }
        }
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
            ReportError("closing brace expected after body");
        }

        return new BlockStmt(body.ToArray());
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
        return new IfStmt(condition, thenStmt);
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
        Expr left = ParsePrimary();
        while (Current.TokenType == TokenType.Multiply || Current.TokenType == TokenType.Divide) {
            Token op = NextToken();
            Expr right = ParsePrimary();
            left = new BinaryExpr(left, op, right);
        }

        return left;

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
                    ReportError($"Expected primary expression, got {token.TokenType} '{token.Text}'");
                    if (Current.TokenType != TokenType.EoF) {
                        NextToken();
                    }
                    return new LiteralExpr(new Token("0", TokenType.Number, 0, token.TextSpan));
                }
        }
    }

    void ReportError(string message) {
        diagnostics.Report(new Diagnostic(message, Severity.Error));
    }

}
