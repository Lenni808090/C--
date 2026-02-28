enum TokenType {
    Return,
    Var,

    Identifier,
    Number,

    Equals,
    Semicolon,

    BinaryOperator,

    EoF,
}


struct Token {
    public string Text;
    public TokenType TokenType;

    public Token(string text, TokenType tokenType) {
        Text = text;
        TokenType = tokenType;
    }
}