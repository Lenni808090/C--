using System.Runtime.CompilerServices;
using CMinus.Compiler;
using CMinus.Compiler.Diagnostics;

namespace CMinus.Compiler.Lexing;

class Lexer {

    char[] data;
    int position;

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
        { "break", TokenType.Break},
    };
    public Lexer(string data, CompilerContext context) {
        this.data = data.ToArray();
        diagnostics = context.diagnostics;
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
        return current;
    }



    public Token[] Lex() {
        List<Token> tokens = new();

        while (true) {
            char c = At();

            if (c == '\0') {
                tokens.Add(newToken(TokenType.EoF, "EoF"));
                break;
            }

            if (char.IsWhiteSpace(c)) {
                Next();
                continue;
            }

            switch (c) {
                case '+': {
                        Next();
                        tokens.Add(newToken(TokenType.Plus, "+"));
                        break;
                    }
                case '-': {
                        Next();
                        tokens.Add(newToken(TokenType.Minus, "-"));
                        break;
                    }
                case '*': {
                        Next();
                        tokens.Add(newToken(TokenType.Multiply, "*"));
                        break;
                    }
                case '/': {
                        Next();
                        tokens.Add(newToken(TokenType.Divide, "/"));
                        break;
                    }
                case '|': {
                        Next();
                        if (At() == '|') {
                            Next();
                            tokens.Add(newToken(TokenType.Or, "||"));
                        }
                        else {
                            ReportError(DiagnosticDescriptors.LexerUnexpectedSinglePipe);
                        }
                        break;
                    }
                case '&': {
                        Next();
                        if (At() == '&') {
                            Next();
                            tokens.Add(newToken(TokenType.And, "&&"));
                        }
                        else {
                            ReportError(DiagnosticDescriptors.LexerUnexpectedSingleAmpersand);
                        }
                        break;
                    }
                case ';': {
                        Next();
                        tokens.Add(newToken(TokenType.Semicolon, ";"));
                        break;
                    }
                case '=': {
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.EqualsEquals, "=="));
                        }
                        else {
                            tokens.Add(newToken(TokenType.Equals, "="));
                        }
                        break;
                    }
                case '!': {
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.NotEquals, "!="));
                        }
                        else {
                            tokens.Add(newToken(TokenType.Bang, "!"));
                        }
                        break;
                    }
                case '<': {
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.LessThenEquals, "<="));
                        }
                        else {
                            tokens.Add(newToken(TokenType.LessThen, "<"));
                        }
                        break;
                    }
                case '>': {
                        Next();
                        if (At() == '=') {
                            Next();
                            tokens.Add(newToken(TokenType.MoreThenEquals, ">="));
                        }
                        else {
                            tokens.Add(newToken(TokenType.MoreThen, ">"));
                        }
                        break;
                    }
                case '(': {
                        Next();
                        tokens.Add(newToken(TokenType.OpenParentheses, "("));
                        break;
                    }
                case ')': {
                        Next();
                        tokens.Add(newToken(TokenType.CloseParentheses, ")"));
                        break;
                    }
                case '{': {
                        Next();
                        tokens.Add(newToken(TokenType.OpenBrace, "{"));
                        break;
                    }
                case '}': {
                        Next();
                        tokens.Add(newToken(TokenType.CloseBrace, "}"));
                        break;
                    }
                default: {
                        if (char.IsNumber(c)) {
                            int start = position;

                            Next();

                            while (char.IsNumber(At())) {
                                Next();
                            }
                            int length = position - start;
                            string number = new string(data, start, length);
                            long parsedLong = long.Parse(number);
                            tokens.Add(newToken(TokenType.Number, number, parsedLong));
                        }
                        else if (char.IsLetter(c)) {
                            int start = position;

                            Next();

                            while (char.IsLetterOrDigit(At()) || At() == '_') {
                                Next();
                            }
                            int length = position - start;
                            string text = new string(data, start, length);
                            if (keywords.TryGetValue(text, out TokenType keywordType)) {
                                tokens.Add(newToken(keywordType, text));
                                continue;
                            }
                            tokens.Add(newToken(TokenType.Identifier, text));
                        }
                        else {
                            ReportError(DiagnosticDescriptors.LexerUnknownCharacter, c);
                            Next();
                        }
                        break;
                    }

            }

        }

        return tokens.ToArray();
    }

    public Token newToken(TokenType tokenType, string text, long? value = null) {
        if (value is long v) {
            return new Token(text, tokenType, v);
        }

        return new Token(text, tokenType);
    }

    void ReportError(DiagnosticDescriptor descriptor, params object[] args) {
        diagnostics.Report(descriptor, args);
    }
}
