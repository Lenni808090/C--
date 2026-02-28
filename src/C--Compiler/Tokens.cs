enum TokenType {
    Return,
    Var,

    Identifier,
    Number,

    Equals,

    OpenParentheses,
    CloseParentheses,

    Semicolon,


    Plus,
    Minus,
    Multiply,
    Divide,


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