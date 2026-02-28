class Parser {
    private readonly Token[] tokens;
    private int position;

    public Parser(Token[] tokens) {
        this.tokens = tokens;
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

        throw new Exception(message);
    }

    bool Match(TokenType type) {
        if (Current.TokenType == type) {
            NextToken();
            return true;
        }

        return false;
    }


    public CompilationUnit ParseUnit() {
        List<Stmt> stmts = new();

        while (Current.TokenType != TokenType.EoF) {
            stmts.Add(ParseStmt());
        }

        return new CompilationUnit(stmts.ToArray());
    }


    Stmt ParseStmt() {
        switch (Current.TokenType) {
            case TokenType.Var: {
                    TypeSyntax type = ParseType();
                    Token identifier = Expect(TokenType.Identifier, "After type declaration an identifier is expected");
                    Expect(TokenType.Equals, "After identifier '=' expected in var declaration");
                    Expr assignedExpr = ParseExpr();
                    Expect(TokenType.Semicolon, "Missing ';' after variable declaration");
                    return new VarDeclarationStmt(type, identifier, assignedExpr);
                }
            case TokenType.Return: {
                    return ParseReturmStmt();
                }

            default: {
                    Expr expr = ParseExpr();
                    Expect(TokenType.Semicolon, "Missing ';' after expression");
                    return new ExpressionStmt(expr);
                }
        }
    }

    Stmt ParseReturmStmt() {
        NextToken();
        Expr returnExpr = ParseExpr();
        Expect(TokenType.Semicolon, "Missing ';' after return");
        return new ReturnStmt(returnExpr);
    }
    TypeSyntax ParseType() {
        Token typeToken = Current;
        NextToken();
        return new IdentifierTypeSyntax(typeToken);
    }

    Expr ParseExpr() {
        return ParseBinaryExpr();
    }

    Expr ParseBinaryExpr() {
        Expr left = ParseMultiplyExpr();

        while (Current.TokenType == TokenType.Plus && Current.TokenType == TokenType.Minus) {
            Token op = NextToken();
            Expr right = ParseMultiplyExpr();
            left = new BinaryExpr(left, op, right);
        }

        return left;
    }

    Expr ParseMultiplyExpr() {
        Expr left = ParsePrimary();
        while (Current.TokenType == TokenType.Multiply && Current.TokenType == TokenType.Divide) {
            Token op = NextToken();
            Expr right = ParseMultiplyExpr();
            left = new BinaryExpr(left, op, right);
        }

        return left;

    }
    Expr ParsePrimary() {
        Token token = Current;
        switch (token.TokenType) {
            case TokenType.Number: {
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
                    throw new Exception($"Expected primary expression, got {token.TokenType} '{token.Text}'");
                }
        }
    }


}