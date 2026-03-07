namespace CMinus.Compiler;

enum TokenType {
    Return,

    True,
    False,

    Mut,

    Identifier,
    Number,

    Equals,


    OpenParentheses,
    CloseParentheses,

    OpenBrace,
    CloseBrace,

    Semicolon,

    If,
    Else,

    While,
    For,

    Continue,
    Break,

    Bang,

    Plus,
    Minus,
    Multiply,
    Divide,

    PlusEquals,
    MinusEquals,
    MultiplyEquals,
    DivideEquals,

    EqualsEquals,
    NotEquals,
    MoreThen,
    LessThen,
    MoreThenEquals,
    LessThenEquals,

    Or,
    And,


    EoF,
}


sealed class Token {
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
