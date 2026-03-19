namespace CMinus.CodeGen;

class CompiledProgram {
    public CompiledFunction[] compiledFunctions;

    public Value[] constants;
    public ushort entryFuncInd;
    public string[] nativeFunctionNames;
    public RuntimeTypeDesc[] typeTable;

    public CompiledProgram(CompiledFunction[] compiledFunctions, Value[] constants, ushort entryFuncInd, RuntimeTypeDesc[] typeTable, string[] nativeFunctionNames) {
        this.constants = constants;
        this.compiledFunctions = compiledFunctions;
        this.entryFuncInd = entryFuncInd;
        this.typeTable = typeTable;
        this.nativeFunctionNames = nativeFunctionNames;
    }
}

class CompiledFunction {
    public byte[] bytecode;
    public int localCount;
    public int paramCount;

    public FuncByteoffsetStackMap functionStackMap;
    public int maxRegCount;

    public CompiledFunction(byte[] bytecode, int localCount, int maxRegCount, int paramCount, FuncByteoffsetStackMap functionStackMap) {
        this.bytecode = bytecode;
        this.paramCount = paramCount;
        this.localCount = localCount;
        this.maxRegCount = maxRegCount;
        this.functionStackMap = functionStackMap;
    }
}
