using System.Runtime.CompilerServices;
using CMinus.Compiler;
using CMinus.Compiler.Diagnostics;

namespace CMinus.Compiler.Lexing;

class Lexer {

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
        {"mut", TokenType.Mut},
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
                        int start = position;
                        int startLine = line;
                        int startColumn = column;
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.PlusEquals, "+=", start, startLine, startColumn));
                        }
                        else {
                            tokens.Add(newToken(TokenType.Plus, "+", start, startLine, startColumn));
                        }
                        break;
                    }
                case '-': {
                        int start = position;
                        int startLine = line;
                        int startColumn = column;
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.MinusEquals, "-=", start, startLine, startColumn));
                        }
                        else {
                            tokens.Add(newToken(TokenType.Minus, "-", start, startLine, startColumn));
                        }
                        break;
                    }
                case ':': {
                        int start = position;
                        int startLine = line;
                        int startColumn = column;

                        tokens.Add(newToken(TokenType.Colon, ":", start, startLine, startColumn));
                        Next();
                        break;
                    }
                case '*': {
                        int start = position;
                        int startLine = line;
                        int startColumn = column;
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.MultiplyEquals, "*=", start, startLine, startColumn));
                        }
                        else {
                            tokens.Add(newToken(TokenType.Multiply, "*", start, startLine, startColumn));
                        }
                        break;
                    }
                case '/': {
                        int start = position;
                        int startLine = line;
                        int startColumn = column;
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.DivideEquals, "/=", start, startLine, startColumn));
                        }
                        else {
                            tokens.Add(newToken(TokenType.Divide, "/", start, startLine, startColumn));
                        }
                        break;
                    }
                case '|': {
                        int start = position;
                        int startLine = line;
                        int startColumn = column;
                        Next();
                        if (At() == '|') {
                            Next();
                            tokens.Add(newToken(TokenType.Or, "||", start, startLine, startColumn));
                        }
                        else {
                            ReportError(CurrentLocation(start, startLine, startColumn), DiagnosticDescriptors.LexerUnexpectedSinglePipe);
                        }
                        break;
                    }
                case '&': {
                        int start = position;
                        int startLine = line;
                        int startColumn = column;
                        Next();
                        if (At() == '&') {
                            Next();
                            tokens.Add(newToken(TokenType.And, "&&", start, startLine, startColumn));
                        }
                        else {
                            ReportError(CurrentLocation(start, startLine, startColumn), DiagnosticDescriptors.LexerUnexpectedSingleAmpersand);
                        }
                        break;
                    }
                case ';': {
                        int start = position;
                        int startLine = line;
                        int startColumn = column;
                        Next();
                        tokens.Add(newToken(TokenType.Semicolon, ";", start, startLine, startColumn));
                        break;
                    }
                case '=': {
                        int start = position;
                        int startLine = line;
                        int startColumn = column;
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.EqualsEquals, "==", start, startLine, startColumn));
                        }
                        else {
                            tokens.Add(newToken(TokenType.Equals, "=", start, startLine, startColumn));
                        }
                        break;
                    }
                case '!': {
                        int start = position;
                        int startLine = line;
                        int startColumn = column;
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.NotEquals, "!=", start, startLine, startColumn));
                        }
                        else {
                            tokens.Add(newToken(TokenType.Bang, "!", start, startLine, startColumn));
                        }
                        break;
                    }
                case '<': {
                        int start = position;
                        int startLine = line;
                        int startColumn = column;
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.LessThenEquals, "<=", start, startLine, startColumn));
                        }
                        else {
                            tokens.Add(newToken(TokenType.LessThen, "<", start, startLine, startColumn));
                        }
                        break;
                    }
                case '>': {
                        int start = position;
                        int startLine = line;
                        int startColumn = column;
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.MoreThenEquals, ">=", start, startLine, startColumn));
                        }
                        else {
                            tokens.Add(newToken(TokenType.MoreThen, ">", start, startLine, startColumn));
                        }
                        break;
                    }
                case '(': {
                        int start = position;
                        int startLine = line;
                        int startColumn = column;
                        Next();
                        tokens.Add(newToken(TokenType.OpenParentheses, "(", start, startLine, startColumn));
                        break;
                    }
                case ')': {
                        int start = position;
                        int startLine = line;
                        int startColumn = column;
                        Next();
                        tokens.Add(newToken(TokenType.CloseParentheses, ")", start, startLine, startColumn));
                        break;
                    }
                case '{': {
                        int start = position;
                        int startLine = line;
                        int startColumn = column;
                        Next();
                        tokens.Add(newToken(TokenType.OpenBrace, "{", start, startLine, startColumn));
                        break;
                    }
                case '}': {
                        int start = position;
                        int startLine = line;
                        int startColumn = column;
                        Next();
                        tokens.Add(newToken(TokenType.CloseBrace, "}", start, startLine, startColumn));
                        break;
                    }
                default: {
                        if (char.IsNumber(c)) {
                            int start = position;
                            int startLine = line;
                            int startColumn = column;

                            Next();

                            while (char.IsNumber(At())) {
                                Next();
                            }
                            int length = position - start;
                            string number = new string(data, start, length);
                            long parsedLong = long.Parse(number);
                            tokens.Add(newToken(TokenType.Number, number, start, startLine, startColumn, parsedLong));
                        }
                        else if (char.IsLetter(c)) {
                            int start = position;
                            int startLine = line;
                            int startColumn = column;

                            Next();

                            while (char.IsLetterOrDigit(At()) || At() == '_') {
                                Next();
                            }
                            int length = position - start;
                            string text = new string(data, start, length);
                            if (keywords.TryGetValue(text, out TokenType keywordType)) {
                                tokens.Add(newToken(keywordType, text, start, startLine, startColumn));
                                continue;
                            }
                            tokens.Add(newToken(TokenType.Identifier, text, start, startLine, startColumn));
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

    SourceLocation CurrentLocation(int start, int startLine, int startColumn) {
        return new SourceLocation(startLine, startColumn, start, position - start);
    }

    public Token newToken(TokenType tokenType, string text, int start, int startLine, int startColumn, long? value = null) {
        SourceLocation location = CurrentLocation(start, startLine, startColumn);
        if (value is long v) {
            return new Token(text, tokenType, location, v);
        }

        return new Token(text, tokenType, location);
    }

    void ReportError(SourceLocation location, DiagnosticDescriptor descriptor, params object[] args) {
        diagnostics.Report(location, descriptor, args);
    }
}
