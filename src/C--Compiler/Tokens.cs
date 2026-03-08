namespace CMinus.Compiler;

enum TokenType {
    Return,

    True,
    False,

    Mut,

    Identifier,
    Number,
    Char,

    Equals,


    OpenParentheses,
    CloseParentheses,

    OpenBrace,
    CloseBrace,

    Semicolon,
    Colon,
    Comma,
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
    Modulus,

    PlusEquals,
    MinusEquals,
    MultiplyEquals,
    DivideEquals,
    ModulusEquals,

    EqualsEquals,
    NotEquals,
    MoreThen,
    LessThen,
    MoreThenEquals,
    LessThenEquals,

    Or,
    And,

    Meth,
    Arrow,

    EoF,
}


sealed class Token {
    public string Text;

    public TokenType TokenType;
    public SourceLocation Location;
    public long Value;
    public bool hasValue;
    public Token(string text, TokenType tokenType, SourceLocation location) {
        Text = text;
        TokenType = tokenType;
        Location = location;
        hasValue = false;
    }

    public Token(string text, TokenType tokenType, SourceLocation location, long value) {
        Text = text;
        TokenType = tokenType;
        Location = location;
        Value = value;
        hasValue = true;
    }
}
