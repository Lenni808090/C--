enum TokenType {
    Return,

    True,
    False,

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


class Token {
    public string Text;
    public TokenType TokenType;
    public long Value;
    public bool hasValue;
    public Token(string text, TokenType tokenType) {
        Text = text;
        TokenType = tokenType;
        hasValue = false;
    }

    public Token(string text, TokenType tokenType, long value) {
        Text = text;
        TokenType = tokenType;
        Value = value;
        hasValue = true;
    }
}