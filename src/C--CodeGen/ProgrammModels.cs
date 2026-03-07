using CMinus.Runtime;

namespace CMinus.CodeGen;

class CompiledProgram {
    public CompiledFunction[] compiledFunctions;

    public int entryFuncInd;
    public CompiledProgram(CompiledFunction[] compiledFunctions, int entryFuncInd) {
        this.compiledFunctions = compiledFunctions;
        this.entryFuncInd = entryFuncInd;
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

    public CallFrame AsCallFrame(ushort? returnReg, int functionInd) {
        Value[] regs = new Value[maxRegCount];
        Value[] locals = new Value[localCount + paramCount];
        return new CallFrame(regs, locals, returnReg, functionInd);
    }
}

