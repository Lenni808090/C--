using CMinus.CodeGen;
using CMinus.Compiler;
using CMinus.Compiler.Binding;
using CMinus.Compiler.Lexing;
using CMinus.Compiler.Lowering;
using CMinus.Compiler.Parsing;
using CMinus.Compiler.Syntax;
using CMinus.Runtime;

namespace CMinus;

class Program {
    static void Main() {
        string code = @"
meth Add(x: int, y: int) -> int {
    return x + y;
}
";

        CompilerContext compilerContext = new();
        var diagnostics = compilerContext.diagnostics;

        Lexer lexer = new Lexer(code, compilerContext);
        Token[] tokens = lexer.Lex();

        if (diagnostics.CheckForErrors()) {
            diagnostics.PrintAllErrors();
            return;
        }

        foreach (Token token in tokens) {
            Console.WriteLine(token.TokenType + " " + token.Text);
        }

        Parser parser = new Parser(tokens, compilerContext);
        CompilationUnit compilationUnit = parser.ParseUnit();

        if (diagnostics.CheckForErrors()) {
            diagnostics.PrintAllErrors();
            return;
        }

        PrintSyntaxUnit(compilationUnit);

        Binder binder = new Binder(compilationUnit, compilerContext);
        BoundCompiledUnit bound = binder.BindCompiledUnit();

        if (diagnostics.CheckForErrors()) {
            diagnostics.PrintAllErrors();
            return;
        }

        PrintBoundUnit(bound);

        IrBuilder irBuilder = new IrBuilder(bound);
        IrCompiledUnit irCompiledUnit = irBuilder.BuildCompiledUnit();
        PrintIrCompiledUnit(irCompiledUnit);

        ControlFlowAnalyser controlFlowAnalyser = new ControlFlowAnalyser(irCompiledUnit, compilerContext);
        irCompiledUnit = controlFlowAnalyser.Analyse();

        PrintIrCompiledUnit(irCompiledUnit);

        if (diagnostics.CheckForErrors()) {
            diagnostics.PrintAllErrors();
            return;
        }

        CodeGenerator codeGenerator = new CodeGenerator(irCompiledUnit);
        CompiledProgram compiledProgram = codeGenerator.GenerateProgram();
        Value[] constants = codeGenerator.Constants;

        Console.WriteLine();
        PrintBytecode(compiledProgram.compiledFunctions[0], constants);

        VM vm = new VM(compiledProgram, constants);
        Value result = vm.Run();
        Console.WriteLine("RESULT: " + result);
    }

    static void PrintSyntaxUnit(CompilationUnit unit) {
        Console.WriteLine("=== SYNTAX TREE ===");
        for (int i = 0; i < unit.stmts.Length; i++) {
            PrintSyntaxStmt(unit.stmts[i], "", i == unit.stmts.Length - 1);
        }
    }

    static void PrintSyntaxStmt(Stmt stmt, string indent, bool isLast) {
        string marker = isLast ? "+--" : "+--";
        Console.Write(indent);
        Console.Write(marker);
        Console.WriteLine(stmt.syntaxKind);

        indent += isLast ? "   " : "|  ";

        switch (stmt) {
            case FunctionDeclarationStmt f: {
                    PrintNameToken("name", f.functionName, indent, false);
                    Console.Write(indent);
                    Console.WriteLine("+--Parameters");
                    PrintSyntaxParameters(f.@params, indent + "|  ");
                    Console.Write(indent);
                    Console.WriteLine("+--ReturnType");
                    PrintSyntaxType(f.returnType, indent + "|  ", false);
                    Console.Write(indent);
                    Console.WriteLine("+--Body");
                    PrintSyntaxStmt(f.functionBody, indent + "   ", true);
                    break;
                }
            case VarDeclarationStmt v: {
                    PrintSyntaxModifiers(v.modifiers, indent, false);
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
                    Console.WriteLine("+--Condition");
                    PrintSyntaxExpr(i.condition, indent + "|  ", true);

                    Console.Write(indent);
                    Console.WriteLine("+--Then");
                    PrintSyntaxStmt(i.thenStmt, indent + "   ", true);
                    if (i.elseStmt is not null) {
                        Console.Write(indent);
                        Console.WriteLine("+--Else");
                        PrintSyntaxStmt(i.elseStmt, indent + "   ", true);
                    }

                    break;
                }
            case WhileStmt w: {
                    Console.Write(indent);
                    Console.WriteLine("+--Condition");
                    PrintSyntaxExpr(w.condition, indent + "   ", true);

                    Console.Write(indent);
                    Console.WriteLine("+--Body");
                    PrintSyntaxStmt(w.body, indent + "   ", true);
                    break;
                }
            case ForStmt f: {
                    if (f.declarationStmt is not null) {
                        Console.Write(indent);
                        Console.WriteLine("+--Declaration");
                        PrintSyntaxStmt(f.declarationStmt, indent + "   ", true);
                    }
                    else if (f.initializeExpr is not null) {
                        Console.Write(indent);
                        Console.WriteLine("+--Initialize");
                        PrintSyntaxExpr(f.initializeExpr, indent + "   ", true);
                    }

                    Console.Write(indent);
                    Console.WriteLine("+--Condition");
                    PrintSyntaxExpr(f.condition, indent + "   ", true);

                    Console.Write(indent);
                    Console.WriteLine("+--Iteration");
                    PrintSyntaxExpr(f.iteration, indent + "   ", true);

                    Console.Write(indent);
                    Console.WriteLine("+--Body");
                    PrintSyntaxStmt(f.body, indent + "   ", true);
                    break;
                }
            case ContinueStmt:
            case BreakStmt: {
                    break;
                }
            case BlockStmt b: {
                    for (int j = 0; j < b.stmts.Length; j++) {
                        PrintSyntaxStmt(b.stmts[j], indent, j == b.stmts.Length - 1);
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
        string marker = isLast ? "+--" : "+--";
        Console.Write(indent);
        Console.Write(marker);
        Console.WriteLine(expr.syntaxKind);

        indent += isLast ? "   " : "|  ";

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
                    Console.WriteLine("+--Operator");
                    Console.Write(indent + "|  ");
                    Console.WriteLine("symbol: " + bin.Operator.Text);

                    PrintSyntaxExpr(bin.rightExpr, indent, true);
                    break;
                }
            case VarAssignmentExpr va: {
                    PrintNameToken("name", va.variable, indent, false);
                    PrintSyntaxExpr(va.assignmentExpr, indent, true);
                    break;
                }
            case UnaryExpr unary: {
                    Console.Write(indent);
                    Console.WriteLine("+--Operator");
                    Console.Write(indent + "   ");
                    Console.WriteLine("symbol: " + unary.Operator.Text);
                    PrintSyntaxExpr(unary.operatedExpr, indent, true);
                    break;
                }
            default: {
                    Console.Write(indent);
                    Console.WriteLine("Unhandled syntax expr: " + expr.GetType().Name);
                    break;
                }
        }
    }

    static void PrintSyntaxType(TypeSyntax type, string indent, bool isLast) {
        string marker = isLast ? "+--" : "+--";
        Console.Write(indent);
        Console.Write(marker);
        Console.WriteLine(type.syntaxKind);

        switch (type) {
            case IdentifierTypeSyntax id: {
                    Console.Write(indent + (isLast ? "   " : "|  "));
                    Console.WriteLine("type: " + id.identifier.Text);
                    break;
                }
        }
    }

    static void PrintSyntaxModifiers(Token[] modifiers, string indent, bool isLast) {
        if (modifiers.Length == 0) {
            return;
        }

        string marker = isLast ? "+--" : "+--";
        Console.Write(indent);
        Console.Write(marker);
        Console.WriteLine("Modifiers");

        string childIndent = indent + (isLast ? "   " : "|  ");
        for (int i = 0; i < modifiers.Length; i++) {
            PrintNameToken("modifier", modifiers[i], childIndent, i == modifiers.Length - 1);
        }
    }

    static void PrintSyntaxParameters(ParameterSyntax[] parameters, string indent) {
        if (parameters.Length == 0) {
            Console.Write(indent);
            Console.WriteLine("+--<none>");
            return;
        }

        for (int i = 0; i < parameters.Length; i++) {
            PrintSyntaxParameter(parameters[i], indent, i == parameters.Length - 1);
        }
    }

    static void PrintSyntaxParameter(ParameterSyntax parameter, string indent, bool isLast) {
        string marker = isLast ? "+--" : "+--";
        Console.Write(indent);
        Console.Write(marker);
        Console.WriteLine(parameter.syntaxKind);

        string childIndent = indent + (isLast ? "   " : "|  ");
        PrintSyntaxModifiers(parameter.modifiers, childIndent, false);
        PrintSyntaxType(parameter.type, childIndent, false);
        PrintNameToken("name", parameter.name, childIndent, true);
    }

    static void PrintNameToken(string label, Token tok, string indent, bool isLast) {
        string marker = isLast ? "+--" : "+--";
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

    static void PrintBytecode(CompiledFunction fn, Value[] constants) {
        Console.WriteLine("=== CONSTANT POOL ===");
        for (int i = 0; i < constants.Length; i++) {
            Console.WriteLine($"[{i}] {constants[i]}");
        }

        Console.WriteLine();
        Console.WriteLine("=== BYTECODE ===");

        byte[] code = fn.bytecode;
        int ip = 0;
        ushort ReadU16() {
            ushort value = BitConverter.ToUInt16(code, ip);
            ip += 2;
            return value;
        }

        while (ip < code.Length) {
            int start = ip;
            OpCode op = (OpCode)code[ip++];

            Console.Write($"{start:D4}: {op}");

            switch (op) {
                case OpCode.LOAD_CONST: {
                        ushort dst = ReadU16();
                        ushort ci = ReadU16();
                        Console.WriteLine($" r{dst}, const[{ci}]");
                        break;
                    }
                case OpCode.LOAD_LOCAL: {
                        ushort dst = ReadU16();
                        ushort li = ReadU16();
                        Console.WriteLine($" r{dst}, local[{li}]");
                        break;
                    }
                case OpCode.STORE_LOCAL: {
                        ushort src = ReadU16();
                        ushort li = ReadU16();
                        Console.WriteLine($" r{src}, local[{li}]");
                        break;
                    }
                case OpCode.RETURN: {
                        ushort r = ReadU16();
                        Console.WriteLine($" r{r}");
                        break;
                    }
                case OpCode.ADD_INT:
                case OpCode.SUBTRACT_INT:
                case OpCode.MULTIPLY_INT:
                case OpCode.DIVIDE_INT:
                case OpCode.CMP_EQ:
                case OpCode.CMP_LT_INT:
                case OpCode.CMP_MT_INT:
                case OpCode.CMP_LTE_INT:
                case OpCode.CMP_MTE_INT:
                case OpCode.CMP_NEQ: {
                        ushort dst = ReadU16();
                        ushort left = ReadU16();
                        ushort right = ReadU16();
                        Console.WriteLine($" r{dst}, r{left}, r{right}");
                        break;
                    }
                case OpCode.NEG_INT:
                case OpCode.NOT:
                case OpCode.MOVE: {
                        ushort dst = ReadU16();
                        ushort src = ReadU16();
                        Console.WriteLine($" r{dst}, r{src}");
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
                        ushort cond = ReadU16();
                        int offset = BitConverter.ToInt32(code, ip);
                        ip += 4;
                        Console.WriteLine($" r{cond}, {offset}");
                        break;
                    }
                case OpCode.CALL: {
                        ushort dst = ReadU16();
                        int functionIndex = BitConverter.ToInt32(code, ip);
                        ip += 4;
                        ushort argCount = ReadU16();
                        Console.Write($" r{dst}, fn[{functionIndex}], argc={argCount}");
                        for (int i = 0; i < argCount; i++) {
                            ushort argReg = ReadU16();
                            Console.Write($" r{argReg}");
                        }
                        Console.WriteLine();
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
        Console.WriteLine($"ParamCount = {fn.paramCount}");
        Console.WriteLine($"MaxRegCount = {fn.maxRegCount}");
    }

    static void PrintBoundStmt(BoundStmt stmt, string indent, bool isLast) {
        string marker = isLast ? "+--" : "+--";
        Console.Write(indent);
        Console.Write(marker);
        Console.WriteLine(stmt.GetType().Name);

        indent += isLast ? "   " : "|  ";

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
            case BoundIfStmt i: {
                    Console.Write(indent);
                    Console.WriteLine("+--Condition");
                    PrintBoundExpr(i.boundConditionExpr, indent + "   ", true);

                    Console.Write(indent);
                    Console.WriteLine("+--Then");
                    PrintBoundStmt(i.thenStmt, indent + "   ", true);
                    if (i.elseStmt is not null) {
                        Console.Write(indent);
                        Console.WriteLine("+--Else");
                        PrintBoundStmt(i.elseStmt, indent + "   ", true);
                    }
                    break;
                }
            case BoundWhileStmt w: {
                    Console.Write(indent);
                    Console.WriteLine("+--Condition");
                    PrintBoundExpr(w.boundConditionExpr, indent + "   ", true);

                    Console.Write(indent);
                    Console.WriteLine("+--Body");
                    PrintBoundStmt(w.body, indent + "   ", true);
                    break;
                }
            case BoundContinueStmt:
            case BoundBreakStmt: {
                    break;
                }
            case BoundErrorStmt: {
                    break;
                }
            case BoundBlockStmt b: {
                    for (int j = 0; j < b.boundStmts.Length; j++) {
                        PrintBoundStmt(b.boundStmts[j], indent, j == b.boundStmts.Length - 1);
                    }
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
        string marker = isLast ? "+--" : "+--";
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
            case BoundVarAssignmentExpr va: {
                    Console.Write(indent);
                    Console.WriteLine($"local: {va.localSymbol.name} : {va.localSymbol.symbolType} (index {va.localSymbol.index})");
                    PrintBoundExpr(va.assignmentExpr, indent, true);
                    break;
                }
            case BoundBinaryExpr bin: {
                    Console.WriteLine($"BoundBinaryExpr : {bin.type}  op={bin.boundBinaryOperator.operatorKind}");
                    indent += isLast ? "   " : "|  ";
                    PrintBoundExpr(bin.leftBoundExpr, indent, false);
                    PrintBoundExpr(bin.rightBoundExpr, indent, true);
                    break;
                }
            case BoundUnaryExpr unary: {
                    Console.WriteLine($"BoundUnaryExpr : {unary.type}  op={unary.boundUnaryOperator.unaryOperatorKind}");
                    indent += isLast ? "   " : "|  ";
                    PrintBoundExpr(unary.operatedExpr, indent, true);
                    break;
                }
            default: {
                    Console.WriteLine($"{expr.GetType().Name} : {expr.type}");
                    break;
                }
        }
    }

    static void PrintIrCompiledUnit(IrCompiledUnit irCompiledUnit) {
        Console.WriteLine("=== IR BLOCKS ===");
        foreach (BasicBlock block in irCompiledUnit.basicBlocks) {
            Console.WriteLine($"block {block.blockId} (unreachable={block.isUnreachable})");

            foreach (IrInstr irInstr in block.irInstrs) {
                switch (irInstr) {
                    case IrLoadConst c:
                        Console.WriteLine($"  load_const r{c.dstReg} <- {c.valueType}({c.rawValue})");
                        break;
                    case IrStoreLocal s:
                        Console.WriteLine($"  store_local local[{s.localIndex}] <- r{s.srcReg}");
                        break;
                    case IrLoadLocal l:
                        Console.WriteLine($"  load_local r{l.dstReg} <- local[{l.localIndex}]");
                        break;
                    case IrMove m:
                        Console.WriteLine($"  move r{m.dstReg} <- r{m.srcReg}");
                        break;
                    case IrBinaryOp b:
                        Console.WriteLine($"  binary {b.irBinaryOP} r{b.dstReg} <- r{b.leftReg}, r{b.rightReg}");
                        break;
                    case IrUnary u:
                        Console.WriteLine($"  unary {u.irUnaryOp} r{u.dstReg} <- r{u.operandReg}");
                        break;
                    default:
                        Console.WriteLine("  <unknown instr>");
                        break;
                }
            }

            if (block.terminator is null) {
                Console.WriteLine("  terminator: <none>");
            }
            else {
                switch (block.terminator) {
                    case IrReturn r:
                        Console.WriteLine($"  terminator: return r{r.returnReg}");
                        break;
                    case IrGoto g:
                        Console.WriteLine($"  terminator: goto block {g.basicBlockId}");
                        break;
                    case IrBranch b:
                        Console.WriteLine($"  terminator: branch r{b.condReg} ? block {b.thenBlockId} : block {b.elseBlockId}");
                        break;
                    default:
                        Console.WriteLine("  terminator: <unknown>");
                        break;
                }
            }
        }
    }
}
