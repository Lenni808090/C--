using System.Runtime.CompilerServices;

class Lexer {

    char[] data;
    int position;
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
                break;
            }

            if (char.IsWhiteSpace(c)) {
                Next();
                continue;
            }

            switch (c) {
                case '+': {
                        Next();
                        tokens.Add(newToken(TokenType.BinaryOperator, "+"));
                        break;
                    }
                case '=': {
                        Next();
                        tokens.Add(newToken(TokenType.Equals, "="));
                        break;
                    }
                default: {
                        if (char.IsNumber(c)) {
                            int start = position;

                            while (char.IsNumber(At())) {

                                Next();
                            }
                            int length = position - start;
                            string number = new string(data, start, length);

                            tokens.Add(newToken(TokenType.Number, number));
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