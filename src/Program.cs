using System;

class Program {
    static void Main() {
        string code = @"int x = 100;
                        int y = 200;
                        int z = (x + y) * 2;
                        return true;";

        Lexer lexer = new Lexer(code);
        Token[] tokens = lexer.Lex();
        Parser parser = new Parser(tokens);
        foreach (Token token in tokens) {
            Console.WriteLine(token.TokenType + " " + token.Text);
        }
        CompilationUnit compilationUnit = parser.ParseUnit();

        Binder binder = new Binder(compilationUnit);
        BoundCompiledUnit bound = binder.BindCompiledUnit();
        PrintBoundUnit(bound);

    }
    static void PrintBoundUnit(BoundCompiledUnit unit) {
        Console.WriteLine("=== BOUND TREE ===");
        for (int i = 0; i < unit.boundStmts.Length; i++) {
            PrintBoundStmt(unit.boundStmts[i], "", i == unit.boundStmts.Length - 1);
        }
    }

    static void PrintBoundStmt(BoundStmt stmt, string indent, bool isLast) {
        string marker = isLast ? "└──" : "├──";
        Console.Write(indent);
        Console.Write(marker);
        Console.WriteLine(stmt.GetType().Name);

        indent += isLast ? "   " : "│  ";

        switch (stmt) {
            case BoundVarDeclarationStmt v: {
                    Console.Write(indent);
                    Console.WriteLine($"local: {v.localSymbol.name} : {v.localSymbol.symbolType} (index {v.localSymbol.index})");
                    PrintBoundExpr(v.initializer, indent, true);
                    break;
                }
            case BoundReturnStmt r: {
                    PrintBoundExpr(r.boundReturnedExpr, indent, true);
                    break;
                }
            case BoundExpressionStmt e: {
                    PrintBoundExpr(e.boundExpr, indent, true);
                    break;
                }
            default: {
                    Console.Write(indent);
                    Console.WriteLine("Unhandled bound stmt");
                    break;
                }
        }
    }

    static void PrintBoundExpr(BoundExpr expr, string indent, bool isLast) {
        string marker = isLast ? "└──" : "├──";
        Console.Write(indent);
        Console.Write(marker);

        switch (expr) {
            case BoundLiteralExpr lit: {
                    Console.WriteLine($"BoundLiteralExpr : {lit.type}  value={lit.value}");
                    break;
                }
            case BoundNameExpr name: {
                    Console.WriteLine($"BoundNameExpr : {name.type}  name={name.localSymbol.name} (index {name.localSymbol.index})");
                    break;
                }
            case BoundBinaryExpr bin: {
                    Console.WriteLine($"BoundBinaryExpr : {bin.type}  op={bin.boundBinaryOperatorKind}");
                    indent += isLast ? "   " : "│  ";
                    PrintBoundExpr(bin.leftBoundExpr, indent, false);
                    PrintBoundExpr(bin.rightBoundExpr, indent, true);
                    break;
                }
            default: {
                    Console.WriteLine($"{expr.GetType().Name} : {expr.type}");
                    break;
                }
        }
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
                    Console.WriteLine("   type: " + type.identifier.Text);
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


