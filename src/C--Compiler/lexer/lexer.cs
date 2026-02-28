using System.Runtime.CompilerServices;

class Lexer {

    char[] data;
    int position;
    private static readonly Dictionary<string, TokenType> keywords =
    new()
    {
        { "return", TokenType.Return },
        { "var", TokenType.Var}
    };
    public Lexer(string data) {
        this.data = data.ToArray();
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
                case ';': {
                        Next();
                        tokens.Add(newToken(TokenType.Semicolon, ";"));
                        break;
                    }
                case '=': {
                        Next();
                        tokens.Add(newToken(TokenType.Equals, "="));
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
                default: {
                        if (char.IsNumber(c)) {
                            int start = position;

                            Next();

                            while (char.IsNumber(At())) {
                                Next();
                            }
                            int length = position - start;
                            string number = new string(data, start, length);

                            tokens.Add(newToken(TokenType.Number, number));
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
                            throw new Exception("unknown data");
                        }
                        break;
                    }

            }

        }

        return tokens.ToArray();
    }

    public Token newToken(TokenType tokenType, string text) {
        return new Token(text, tokenType);
    }
}