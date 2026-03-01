using System;

class Program {
    static void Main() {
        string code = @"if(x + 1 < 1 && x - 1 > 2){
                            int x = 100;
                            int y = 200;
                            int z = (x + y) * 2;
                            return (z + x) * 3;
                        }";

        Lexer lexer = new Lexer(code);
        Token[] tokens = lexer.Lex();
        Parser parser = new Parser(tokens);
        foreach (Token token in tokens) {
            Console.WriteLine(token.TokenType + " " + token.Text);
        }
        CompilationUnit compilationUnit = parser.ParseUnit();
        PrintSyntaxUnit(compilationUnit);

        // Binder binder = new Binder(compilationUnit);
        // BoundCompiledUnit bound = binder.BindCompiledUnit();
        // PrintBoundUnit(bound);

        // CodeGenerator codeGenerator = new CodeGenerator(bound);
        // CompiledFunction compiledFunction = codeGenerator.GenerateFunction();
        // Console.WriteLine();
        // PrintBytecode(compiledFunction);

        // Value[] regs = new Value[compiledFunction.maxRegCount];
        // VM vm = new VM(
        //     regs,
        //     compiledFunction.constants,
        //     compiledFunction.localCount,
        //     compiledFunction.bytecode
        // );

        // Value result = vm.Run();
        // Console.WriteLine("RESULT: " + result);

    }
    static void PrintSyntaxUnit(CompilationUnit unit) {
        Console.WriteLine("=== SYNTAX TREE ===");
        for (int i = 0; i < unit.stmts.Length; i++) {
            PrintSyntaxStmt(unit.stmts[i], "", i == unit.stmts.Length - 1);
        }
    }

    static void PrintSyntaxStmt(Stmt stmt, string indent, bool isLast) {
        string marker = isLast ? "└──" : "├──";
        Console.Write(indent);
        Console.Write(marker);
        Console.WriteLine(stmt.syntaxKind);

        indent += isLast ? "   " : "│  ";

        switch (stmt) {
            case VarDeclarationStmt v: {
                    PrintSyntaxType(v.type, indent, false);
                    PrintNameToken("name", v.name, indent, false);
                    PrintSyntaxExpr(v.declarementExpr, indent, true);
                    break;
                }
            case ReturnStmt r: {
                    PrintSyntaxExpr(r.returnExpr, indent, true);
                    break;
                }
            case ExpressionStmt e: {
                    PrintSyntaxExpr(e.Expression, indent, true);
                    break;
                }
            case IfStmt i: {
                    Console.Write(indent);
                    Console.WriteLine("├──Condition");
                    PrintSyntaxExpr(i.condition, indent + "│  ", true);

                    Console.Write(indent);
                    Console.WriteLine("└──Body");

                    for (int j = 0; j < i.body.Length; j++) {
                        PrintSyntaxStmt(i.body[j], indent + "   ", j == i.body.Length - 1);
                    }

                    break;
                }
            default: {
                    Console.Write(indent);
                    Console.WriteLine("Unhandled syntax stmt: " + stmt.GetType().Name);
                    break;
                }
        }
    }

    static void PrintSyntaxExpr(Expr expr, string indent, bool isLast) {
        string marker = isLast ? "└──" : "├──";
        Console.Write(indent);
        Console.Write(marker);
        Console.WriteLine(expr.syntaxKind);

        indent += isLast ? "   " : "│  ";

        switch (expr) {
            case LiteralExpr lit: {
                    Console.Write(indent);
                    Console.WriteLine("value: " + lit.value.Text);
                    break;
                }
            case NameExpr name: {
                    Console.Write(indent);
                    Console.WriteLine("name: " + name.name.Text);
                    break;
                }
            case BinaryExpr bin: {
                    PrintSyntaxExpr(bin.leftExpr, indent, false);

                    Console.Write(indent);
                    Console.WriteLine("├──Operator");
                    Console.Write(indent + "│  ");
                    Console.WriteLine("symbol: " + bin.Operator.Text);

                    PrintSyntaxExpr(bin.rightExpr, indent, true);
                    break;
                }
            // If you add unary:
            // case UnaryExpr u: { ... }
            default: {
                    Console.Write(indent);
                    Console.WriteLine("Unhandled syntax expr: " + expr.GetType().Name);
                    break;
                }
        }
    }

    static void PrintSyntaxType(TypeSyntax type, string indent, bool isLast) {
        string marker = isLast ? "└──" : "├──";
        Console.Write(indent);
        Console.Write(marker);
        Console.WriteLine(type.syntaxKind);

        switch (type) {
            case IdentifierTypeSyntax id: {
                    Console.Write(indent + (isLast ? "   " : "│  "));
                    Console.WriteLine("type: " + id.identifier.Text);
                    break;
                }
        }
    }

    static void PrintNameToken(string label, Token tok, string indent, bool isLast) {
        string marker = isLast ? "└──" : "├──";
        Console.Write(indent);
        Console.Write(marker);
        Console.WriteLine(label + ": " + tok.Text);
    }
    static void PrintBoundUnit(BoundCompiledUnit unit) {
        Console.WriteLine("=== BOUND TREE ===");
        for (int i = 0; i < unit.boundStmts.Length; i++) {
            PrintBoundStmt(unit.boundStmts[i], "", i == unit.boundStmts.Length - 1);
        }
    }

    static void PrintBytecode(CompiledFunction fn) {
        Console.WriteLine("=== CONSTANT POOL ===");
        for (int i = 0; i < fn.constants.Length; i++) {
            Console.WriteLine($"[{i}] {fn.constants[i]}");
        }

        Console.WriteLine();
        Console.WriteLine("=== BYTECODE ===");

        byte[] code = fn.bytecode;
        int ip = 0;

        while (ip < code.Length) {
            int start = ip;
            OpCode op = (OpCode)code[ip++];

            Console.Write($"{start:D4}: {op}");

            switch (op) {
                case OpCode.LOAD_CONST: {
                        byte dst = code[ip++];
                        byte ci = code[ip++];
                        Console.WriteLine($" r{dst}, const[{ci}]");
                        break;
                    }
                case OpCode.LOAD_LOCAL: {
                        byte dst = code[ip++];
                        byte li = code[ip++];
                        Console.WriteLine($" r{dst}, local[{li}]");
                        break;
                    }
                case OpCode.STORE_LOCAL: {
                        byte src = code[ip++];
                        byte li = code[ip++];
                        Console.WriteLine($" r{src}, local[{li}]");
                        break;
                    }
                case OpCode.RETURN: {
                        byte r = code[ip++];
                        Console.WriteLine($" r{r}");
                        break;
                    }
                case OpCode.ADD_INT:
                case OpCode.SUBTRACT_INT:
                case OpCode.MULTIPLY_INT:
                case OpCode.DIVIDE_INT:
                case OpCode.CMP_EQ_INT:
                case OpCode.CMP_LT_INT:
                case OpCode.CMP_MT_INT: {
                        byte dst = code[ip++];
                        byte left = code[ip++];
                        byte right = code[ip++];
                        Console.WriteLine($" r{dst}, r{left}, r{right}");
                        break;
                    }
                case OpCode.JUMP: {
                        int offset = BitConverter.ToInt32(code, ip);
                        ip += 4;
                        Console.WriteLine($" {offset}");
                        break;
                    }
                case OpCode.JUMP_IF_FALSE:
                case OpCode.JUMP_IF_TRUE: {
                        byte cond = code[ip++];
                        int offset = BitConverter.ToInt32(code, ip);
                        ip += 4;
                        Console.WriteLine($" r{cond}, {offset}");
                        break;
                    }
                default: {
                        Console.WriteLine();
                        break;
                    }
            }
        }

        Console.WriteLine();
        Console.WriteLine($"LocalCount = {fn.localCount}");
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

}


