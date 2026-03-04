using CMinus.Compiler.Diagnostics;

namespace CMinus.Compiler;

enum TokenType {
    Return,

    True,
    False,

    Identifier,
    Number,

    Equals,

    Bang,

    OpenParentheses,
    CloseParentheses,

    OpenBrace,
    CloseBrace,

    Semicolon,

    If,
    Else,

    Plus,
    Minus,
    Multiply,
    Divide,

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


class Token {
    public string Text;

    public TextSpan TextSpan;
    public TokenType TokenType;
    public long Value;
    public bool hasValue;
    public Token(string text, TokenType tokenType, TextSpan textSpan) {
        Text = text;
        TokenType = tokenType;
        hasValue = false;
        TextSpan = textSpan;
    }

    public Token(string text, TokenType tokenType, long value, TextSpan textSpan) {
        Text = text;
        TokenType = tokenType;
        Value = value;
        hasValue = true;
        TextSpan = textSpan;
    }
}
