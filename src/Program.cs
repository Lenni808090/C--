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
                        meth Main() -> int {
                            x: int[] = new int[5];
                            fillArray(x);

                            return x[Echo(4)];
                        }

                        meth fillArray(x: int[]) -> int{
                            for(mut i: int = 0; i < 5; i += 1){
                                x[i] = i;
                            }
                            return 0;
                        }

                        meth Echo(echo: int) -> int {
                            return echo;
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
        CompiledProgram compiledProgram = codeGenerator.GenerateProgramm();
        Value[] constants = codeGenerator.Constants;

        Console.WriteLine();
        PrintProgramBytecode(compiledProgram, constants);

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
            case CallExpr call: {
                    Console.Write(indent);
                    Console.WriteLine("+--Callee");
                    PrintSyntaxExpr(call.calle, indent + "   ", true);

                    Console.Write(indent);
                    Console.WriteLine("+--Arguments");
                    PrintSyntaxArguments(call.args, indent + "   ");
                    break;
                }
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
            case AssignmentExpr va: {
                    Console.Write(indent);
                    Console.WriteLine("+--Target");
                    PrintSyntaxExpr(va.target, indent + "   ", false);
                    PrintSyntaxExpr(va.value, indent, true);
                    break;
                }
            case ArrayCreationExpr arrayCreation: {
                    Console.Write(indent);
                    Console.WriteLine("+--Type");
                    PrintSyntaxType(arrayCreation.typeSyntax, indent + "   ", false);

                    Console.Write(indent);
                    Console.WriteLine("+--Length");
                    PrintSyntaxExpr(arrayCreation.length, indent + "   ", true);
                    break;
                }
            case IndexExpr indexExpr: {
                    Console.Write(indent);
                    Console.WriteLine("+--Target");
                    PrintSyntaxExpr(indexExpr.target, indent + "   ", false);

                    Console.Write(indent);
                    Console.WriteLine("+--Index");
                    PrintSyntaxExpr(indexExpr.index, indent + "   ", true);
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

    static void PrintSyntaxArguments(Expr[] args, string indent) {
        if (args.Length == 0) {
            Console.Write(indent);
            Console.WriteLine("+--<none>");
            return;
        }

        for (int i = 0; i < args.Length; i++) {
            PrintSyntaxExpr(args[i], indent, i == args.Length - 1);
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
            case ArrayTypeSyntax arrayType: {
                    PrintSyntaxType(arrayType.elementType, indent + (isLast ? "   " : "|  "), true);
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
        for (int i = 0; i < unit.functions.Length; i++) {
            var function = unit.functions[i];
            bool isEntry = ReferenceEquals(function, unit.mainFunction);
            Console.WriteLine($"Function {function.functionSymbol.name} (entry={isEntry})");
            PrintBoundFunction(function, "", true);
        }
    }

    static void PrintBoundFunction(BoundFunctionDeclaration function, string indent, bool isLast) {
        string marker = isLast ? "+--" : "+--";
        Console.Write(indent);
        Console.Write(marker);
        Console.WriteLine($"BoundFunctionDeclaration : {function.functionSymbol.returnType}");

        string childIndent = indent + (isLast ? "   " : "|  ");
        Console.Write(childIndent);
        Console.WriteLine($"name: {function.functionSymbol.name}");
        Console.Write(childIndent);
        Console.WriteLine($"args: {function.functionSymbol.argCount}");
        Console.Write(childIndent);
        Console.WriteLine($"locals: {function.functionSymbol.localCount}");
        PrintBoundStmt(function.functionBody, childIndent, true);
    }

    static void PrintBytecode(CompiledFunction fn, Value[] constants) {
        Console.WriteLine("=== BYTECODE ===");

        byte[] code = fn.bytecode;
        int ip = 0;
        ushort ReadU16() {
            if (ip + sizeof(ushort) > code.Length) {
                throw new InvalidOperationException($"Truncated bytecode while reading u16 at offset {ip}.");
            }
            ushort value = BitConverter.ToUInt16(code, ip);
            ip += 2;
            return value;
        }
        int ReadI32() {
            if (ip + sizeof(int) > code.Length) {
                throw new InvalidOperationException($"Truncated bytecode while reading i32 at offset {ip}.");
            }
            int value = BitConverter.ToInt32(code, ip);
            ip += 4;
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
                case OpCode.MODULUS_INT:
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
                        int offset = ReadI32();
                        Console.WriteLine($" {offset}");
                        break;
                    }
                case OpCode.JUMP_IF_FALSE:
                case OpCode.JUMP_IF_TRUE: {
                        ushort cond = ReadU16();
                        int offset = ReadI32();
                        Console.WriteLine($" r{cond}, {offset}");
                        break;
                    }
                case OpCode.CALL: {
                        ushort dst = ReadU16();
                        int functionIndex = ReadI32();
                        ushort argCount = ReadU16();
                        Console.Write($" r{dst}, fn[{functionIndex}], argc={argCount}");
                        for (int i = 0; i < argCount; i++) {
                            ushort argReg = ReadU16();
                            Console.Write($" r{argReg}");
                        }
                        Console.WriteLine();
                        break;
                    }
                case OpCode.NEW_ARRAY: {
                        ushort dst = ReadU16();
                        int typeId = ReadI32();
                        ushort lengthReg = ReadU16();
                        Console.WriteLine($" r{dst}, type[{typeId}], r{lengthReg}");
                        break;
                    }
                case OpCode.STORE_ELEMENT: {
                        ushort src = ReadU16();
                        ushort array = ReadU16();
                        ushort index = ReadU16();
                        Console.WriteLine($" r{src}, r{array}, r{index}");
                        break;
                    }
                case OpCode.LOAD_ELEMNT: {
                        ushort dst = ReadU16();
                        ushort array = ReadU16();
                        ushort index = ReadU16();
                        Console.WriteLine($" r{dst}, r{array}, r{index}");
                        break;
                    }
                case OpCode.ARRAY_LENGTH: {
                        ushort dst = ReadU16();
                        ushort array = ReadU16();
                        Console.WriteLine($" r{dst}, r{array}");
                        break;
                    }
                default: {
                        Console.WriteLine(" <unknown opcode>");
                        return;
                    }
            }
        }

        Console.WriteLine();
        Console.WriteLine($"LocalCount = {fn.localCount}");
        Console.WriteLine($"ParamCount = {fn.paramCount}");
        Console.WriteLine($"MaxRegCount = {fn.maxRegCount}");
    }

    static void PrintProgramBytecode(CompiledProgram program, Value[] constants) {
        Console.WriteLine("=== CONSTANT POOL ===");
        for (int i = 0; i < constants.Length; i++) {
            Console.WriteLine($"[{i}] {constants[i]}");
        }

        Console.WriteLine();
        Console.WriteLine("=== TYPE TABLE ===");
        for (int i = 0; i < program.typeTable.Length; i++) {
            Console.WriteLine($"[{i}] {program.typeTable[i]}");
        }

        Console.WriteLine();
        for (int i = 0; i < program.compiledFunctions.Length; i++) {
            Console.WriteLine($"=== FUNCTION {i} (entry={i == program.entryFuncInd}) ===");
            PrintBytecode(program.compiledFunctions[i], constants);
            Console.WriteLine();
        }
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
                    Console.WriteLine($"local: {v.localSymbol.name} : {v.localSymbol.typeSymbol} (index {v.localSymbol.index})");
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
                    Console.WriteLine($"BoundVarAssignmentExpr : {va.type}");
                    indent += isLast ? "   " : "|  ";
                    Console.Write(indent);
                    Console.WriteLine($"local: {va.localSymbol.name} : {va.localSymbol.typeSymbol} (index {va.localSymbol.index})");
                    PrintBoundExpr(va.value, indent, true);
                    break;
                }
            case BoundIndexAssignmentExpr indexAssignment: {
                    Console.WriteLine($"BoundIndexAssignmentExpr : {indexAssignment.type}");
                    indent += isLast ? "   " : "|  ";
                    Console.Write(indent);
                    Console.WriteLine("+--Target");
                    PrintBoundExpr(indexAssignment.target, indent + "   ", false);
                    Console.Write(indent);
                    Console.WriteLine("+--Index");
                    PrintBoundExpr(indexAssignment.index, indent + "   ", false);
                    Console.Write(indent);
                    Console.WriteLine("+--Value");
                    PrintBoundExpr(indexAssignment.value, indent + "   ", true);
                    break;
                }
            case BoundArrayCreationExpr arrayCreation: {
                    Console.WriteLine($"BoundArrayCreationExpr : {arrayCreation.type}");
                    indent += isLast ? "   " : "|  ";
                    PrintBoundExpr(arrayCreation.length, indent, true);
                    break;
                }
            case BoundIndexExpr indexExpr: {
                    Console.WriteLine($"BoundIndexExpr : {indexExpr.type}");
                    indent += isLast ? "   " : "|  ";
                    PrintBoundExpr(indexExpr.target, indent, false);
                    PrintBoundExpr(indexExpr.index, indent, true);
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
            case BoundCallExpr call: {
                    Console.WriteLine($"BoundCallExpr : {call.type}  callee={call.callee.name}");
                    indent += isLast ? "   " : "|  ";
                    for (int i = 0; i < call.args.Length; i++) {
                        PrintBoundExpr(call.args[i], indent, i == call.args.Length - 1);
                    }
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
        for (int functionIndex = 0; functionIndex < irCompiledUnit.irFunctions.Length; functionIndex++) {
            IrFunction irFunction = irCompiledUnit.irFunctions[functionIndex];
            Console.WriteLine($"Function {functionIndex} (entry={functionIndex == irCompiledUnit.mainFunctionInd}, locals={irFunction.localCount}, params={irFunction.paramCount}, maxRegs={irFunction.maxVReg})");
            foreach (BasicBlock block in irFunction.basicBlocks) {
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
                        case IrCallInstr c:
                            Console.WriteLine($"  call r{c.dstReg} <- fn[{c.functionIndex}]({string.Join(", ", c.argRegs.Select(static r => $"r{r}"))})");
                            break;
                        case IrNewArray a:
                            Console.WriteLine($"  new_array r{a.dstReg} <- type[{a.typeId}], r{a.lengthReg}");
                            break;
                        case IrStoreElement s:
                            Console.WriteLine($"  store_element r{s.srcReg} -> r{s.arrayReg}[r{s.indexReg}]");
                            break;
                        case IrLoadElement l:
                            Console.WriteLine($"  load_element r{l.dstReg} <- r{l.arrayReg}[r{l.indexReg}]");
                            break;
                        case IrArrayLength a:
                            Console.WriteLine($"  array_length r{a.dstReg} <- len(r{a.arrayReg})");
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
}
