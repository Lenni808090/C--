using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using CMinus.Compiler;
using CMinus.Compiler.Diagnostics;

namespace CMinus.Compiler.Lexing;

class Lexer {
    readonly struct TokenStart {
        public int Position {
            get;
        }
        public int Line {
            get;
        }
        public int Column {
            get;
        }

        public TokenStart(int position, int line, int column) {
            Position = position;
            Line = line;
            Column = column;
        }
    }

    char[] data;
    int position;
    int line;
    int column;

    DiagnosticBag diagnostics;
    private static readonly Dictionary<string, TokenType> keywords =
    new()
    {
        { "return", TokenType.Return },
        { "true", TokenType.True},
        { "false", TokenType.False},
        { "if", TokenType.If},
        { "else", TokenType.Else},
        { "while", TokenType.While},
        { "continue", TokenType.Continue},
        { "for", TokenType.For},
        { "break", TokenType.Break},
        { "mut", TokenType.Mut},
        { "meth", TokenType.Meth},
    };
    public Lexer(string data, CompilerContext context) {
        this.data = data.ToArray();
        diagnostics = context.diagnostics;
        line = 1;
        column = 1;
    }

    public char Peek() {
        if (position + 1 >= data.Length) {
            return '\0';
        }

        return data[position + 1];
    }

    public char At() {
        if (position >= data.Length) {
            return '\0';
        }

        return data[position];
    }

    public char Next() {
        char current = At();
        position++;
        if (current == '\n') {
            line++;
            column = 1;
        }
        else {
            column++;
        }
        return current;
    }

    public Token[] Lex() {
        List<Token> tokens = new();

        while (true) {
            char c = At();

            if (c == '\0') {
                tokens.Add(new Token("EoF", TokenType.EoF, new SourceLocation(line, column, position, 0)));
                break;
            }

            if (char.IsWhiteSpace(c)) {
                Next();
                continue;
            }

            switch (c) {
                case '+': {
                        var start = CaptureStart();
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.PlusEquals, "+=", start));
                        }
                        else {
                            tokens.Add(newToken(TokenType.Plus, "+", start));
                        }
                        break;
                    }
                case '-': {
                        var start = CaptureStart();
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.MinusEquals, "-=", start));
                        }
                        else if (At() == '>') {
                            Next();
                            tokens.Add(newToken(TokenType.Arrow, "->", start));
                        }
                        else {
                            tokens.Add(newToken(TokenType.Minus, "-", start));
                        }
                        break;
                    }
                case ':': {
                        var start = CaptureStart();

                        tokens.Add(newToken(TokenType.Colon, ":", start));
                        Next();
                        break;
                    }
                case ',': {
                        var start = CaptureStart();

                        tokens.Add(newToken(TokenType.Comma, ",", start));
                        Next();
                        break;
                    }
                case '*': {
                        var start = CaptureStart();
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.MultiplyEquals, "*=", start));
                        }
                        else {
                            tokens.Add(newToken(TokenType.Multiply, "*", start));
                        }
                        break;
                    }
                case '%': {
                        var start = CaptureStart();
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.ModulusEquals, "%=", start));
                        }
                        else {
                            tokens.Add(newToken(TokenType.Modulus, "%", start));
                        }
                        break;
                    }
                case '/': {
                        var start = CaptureStart();
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.DivideEquals, "/=", start));
                        }
                        else {
                            tokens.Add(newToken(TokenType.Divide, "/", start));
                        }
                        break;
                    }
                case '|': {
                        var start = CaptureStart();
                        Next();
                        if (At() == '|') {
                            Next();
                            tokens.Add(newToken(TokenType.Or, "||", start));
                        }
                        else {
                            ReportError(CurrentLocation(start), DiagnosticDescriptors.LexerUnexpectedSinglePipe);
                        }
                        break;
                    }
                case '&': {
                        var start = CaptureStart();
                        Next();
                        if (At() == '&') {
                            Next();
                            tokens.Add(newToken(TokenType.And, "&&", start));
                        }
                        else {
                            ReportError(CurrentLocation(start), DiagnosticDescriptors.LexerUnexpectedSingleAmpersand);
                        }
                        break;
                    }
                case ';': {
                        var start = CaptureStart();
                        Next();
                        tokens.Add(newToken(TokenType.Semicolon, ";", start));
                        break;
                    }
                case '=': {
                        var start = CaptureStart();
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.EqualsEquals, "==", start));
                        }
                        else {
                            tokens.Add(newToken(TokenType.Equals, "=", start));
                        }
                        break;
                    }
                case '!': {
                        var start = CaptureStart();
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.NotEquals, "!=", start));
                        }
                        else {
                            tokens.Add(newToken(TokenType.Bang, "!", start));
                        }
                        break;
                    }
                case '<': {
                        var start = CaptureStart();
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.LessThenEquals, "<=", start));
                        }
                        else {
                            tokens.Add(newToken(TokenType.LessThen, "<", start));
                        }
                        break;
                    }
                case '>': {
                        var start = CaptureStart();
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.MoreThenEquals, ">=", start));
                        }
                        else {
                            tokens.Add(newToken(TokenType.MoreThen, ">", start));
                        }
                        break;
                    }
                case '(': {
                        var start = CaptureStart();
                        Next();
                        tokens.Add(newToken(TokenType.OpenParentheses, "(", start));
                        break;
                    }
                case ')': {
                        var start = CaptureStart();
                        Next();
                        tokens.Add(newToken(TokenType.CloseParentheses, ")", start));
                        break;
                    }
                case '{': {
                        var start = CaptureStart();
                        Next();
                        tokens.Add(newToken(TokenType.OpenBrace, "{", start));
                        break;
                    }
                case '}': {
                        var start = CaptureStart();
                        Next();
                        tokens.Add(newToken(TokenType.CloseBrace, "}", start));
                        break;
                    }
                case '\'': {
                        var start = CaptureStart();
                        Next();
                        char value;
                        if (At() == '\\') {
                            Next();
                            if (!TryReadEscapedChar(out value)) {
                                ReportError(CurrentLocation(start), DiagnosticDescriptors.LexerCharLiteratureTooLong);
                                continue;
                            }
                            Next();
                        }
                        else {
                            value = Next();
                        }
                        if (At() != '\'') {
                            if (!SkipUntilCLosingQuote(start)) {
                                continue;
                            }

                            Next();
                            ReportError(CurrentLocation(start), DiagnosticDescriptors.LexerCharLiteratureTooLong);
                            continue;
                        }
                        Next();
                        tokens.Add(newToken(TokenType.Char, value.ToString(), start, value));
                        break;
                    }
                default: {
                        if (char.IsNumber(c)) {
                            var start = CaptureStart();

                            Next();

                            while (char.IsNumber(At())) {
                                Next();
                            }
                            int length = position - start.Position;
                            string number = new string(data, start.Position, length);
                            long parsedLong = long.Parse(number);
                            tokens.Add(newToken(TokenType.Number, number, start, parsedLong));
                        }
                        else if (char.IsLetter(c)) {
                            var start = CaptureStart();

                            Next();

                            while (char.IsLetterOrDigit(At()) || At() == '_') {
                                Next();
                            }
                            int length = position - start.Position;
                            string text = new string(data, start.Position, length);
                            if (keywords.TryGetValue(text, out TokenType keywordType)) {
                                tokens.Add(newToken(keywordType, text, start));
                                continue;
                            }
                            tokens.Add(newToken(TokenType.Identifier, text, start));
                        }
                        else {
                            var location = new SourceLocation(line, column, position, 1);
                            ReportError(location, DiagnosticDescriptors.LexerUnknownCharacter, c);
                            Next();
                        }
                        break;
                    }

            }

        }

        return tokens.ToArray();
    }

    TokenStart CaptureStart() {
        return new TokenStart(position, line, column);
    }

    SourceLocation CurrentLocation(TokenStart start) {
        return new SourceLocation(start.Line, start.Column, start.Position, position - start.Position);
    }

    bool SkipUntilCLosingQuote(TokenStart start) {
        while (At() != '\'') {
            if (At() == '\0') {
                ReportError(CurrentLocation(start), DiagnosticDescriptors.LexerCharLiteratureNotClosed);
                return false;
            }
            Next();
        }
        return true;
    }
    bool TryReadEscapedChar(out char value) {
        switch (At()) {
            case 'n':
                value = '\n';
                return true;
            case 't':
                value = '\t';
                return true;
            case 'r':
                value = '\r';
                return true;
            case '0':
                value = '\0';
                return true;
            case '\\':
                value = '\\';
                return true;
            case '\'':
                value = '\'';
                return true;
            case '"':
                value = '"';
                return true;
            default:
                value = default;
                return false;
        }
    }

    Token newToken(TokenType tokenType, string text, TokenStart start, long? value = null) {
        SourceLocation location = CurrentLocation(start);
        if (value is long v) {
            return new Token(text, tokenType, location, v);
        }

        return new Token(text, tokenType, location);
    }

    void ReportError(SourceLocation location, DiagnosticDescriptor descriptor, params object[] args) {
        diagnostics.Report(location, descriptor, args);
    }
}
