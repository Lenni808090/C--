namespace CMinus.CodeGen;

class CompiledProgram {
    public CompiledFunction[] compiledFunctions;

    public Value[] constants;
    public int entryFuncInd;

    public FuncByteoffsetStackMap[] stackMap;
    public string[] nativeFunctionNames;
    public RuntimeTypeDesc[] typeTable;

    public CompiledProgram(CompiledFunction[] compiledFunctions, Value[] constants, int entryFuncInd, RuntimeTypeDesc[] typeTable, string[] nativeFunctionNames, FuncByteoffsetStackMap[] stackMap) {
        this.constants = constants;
        this.compiledFunctions = compiledFunctions;
        this.entryFuncInd = entryFuncInd;
        this.typeTable = typeTable;
        this.nativeFunctionNames = nativeFunctionNames;
        this.stackMap = stackMap;
    }
}

class CompiledFunction {
    public byte[] bytecode;
    public int localCount;
    public int paramCount;
    public int maxRegCount;

    public CompiledFunction(byte[] bytecode, int localCount, int maxRegCount, int paramCount) {
        this.bytecode = bytecode;
        this.paramCount = paramCount;
        this.localCount = localCount;
        this.maxRegCount = maxRegCount;
    }
}
