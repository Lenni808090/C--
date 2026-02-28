using System;

class Program {
    static void Main() {
        Lexer lexer = new Lexer("var x = 100;");
        Token[] tokens = lexer.Lex();
        foreach (Token token in tokens) {
            Console.WriteLine(token.TokenType + " " + token.Text);
        }


    }
}


