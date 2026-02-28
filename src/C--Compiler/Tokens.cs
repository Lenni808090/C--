enum TokenType {
    Number,
    Equals,
    BinaryOperator,
}


struct Token {
    public string Text;
    public TokenType TokenType;

    public Token(string text, TokenType tokenType) {
        Text = text;
        TokenType = tokenType;
    }
}