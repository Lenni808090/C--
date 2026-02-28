using System;

class Program {
    static void Main() {
        string code = @"var x = 100;
                        var y = 200;
                        return x + y;";
        Lexer lexer = new Lexer(code);
        Token[] tokens = lexer.Lex();
        Parser parser = new Parser(tokens);
        foreach (Token token in tokens) {
            Console.WriteLine(token.TokenType + " " + token.Text);
        }
        CompilationUnit compilationUnit = parser.ParseUnit();
        Print(compilationUnit);

    }

    static void Print(SyntaxNode node, string indent = "", bool isLast = true) {
        string marker = isLast ? "└──" : "├──";
        Console.Write(indent);
        Console.Write(marker);
        Console.WriteLine(node.syntaxKind);

        indent += isLast ? "   " : "│  ";

        switch (node) {
            case CompilationUnit unit: {
                    for (int i = 0; i < unit.stmts.Length; i++) {
                        Print(unit.stmts[i], indent, i == unit.stmts.Length - 1);
                    }
                    break;
                }

            case VarDeclarationStmt varDecl: {
                    Print(varDecl.type, indent, false);
                    Print(new NameExpr(varDecl.name), indent, false);
                    Print(varDecl.declarementExpr, indent, true);
                    break;

                }

            case ReturnStmt ret: {
                    Print(ret.returnExpr, indent, true);
                    break;
                }

            case ExpressionStmt exprStmt: {
                    Print(exprStmt.Expression, indent, true);
                    break;
                }

            case BinaryExpr bin: {
                    Print(bin.leftExpr, indent, false);

                    PrintOperator(bin.Operator, indent, false);

                    Print(bin.rightExpr, indent, true);
                    break;

                }

            case LiteralExpr lit: {
                    Console.Write(indent);
                    Console.WriteLine("   value: " + lit.value.Text);
                    break;
                }

            case NameExpr name: {
                    Console.Write(indent);
                    Console.WriteLine("   name: " + name.name.Text);
                    break;
                }

            case IdentifierTypeSyntax type: {
                    Console.Write(indent);
                    Console.WriteLine("   type: " + type.Identifier.Text);
                    break;
                }
        }
    }
    static void PrintOperator(Token op, string indent, bool isLast) {
        string marker = isLast ? "└──" : "├──";

        Console.Write(indent);
        Console.Write(marker);
        Console.WriteLine("Operator");

        indent += isLast ? "   " : "│     ";

        Console.Write(indent);
        Console.WriteLine("symbol: " + op.Text);
    }
}


