using CMinus.Runtime;
using Microsoft.VisualBasic;

namespace CMinus.CodeGen;

class CompiledProgram {
    public CompiledFunction[] compiledFunctions;

    public Value[] constants;
    public int entryFuncInd;
    public RuntimeTypeDesc[] typeTable;

    public CompiledProgram(CompiledFunction[] compiledFunctions, Value[] constants, int entryFuncInd, RuntimeTypeDesc[] typeTable) {
        this.constants = constants;
        this.compiledFunctions = compiledFunctions;
        this.entryFuncInd = entryFuncInd;
        this.typeTable = typeTable;
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
        Value[] locals = new Value[localCount];
        return new CallFrame(regs, locals, returnReg, functionInd);
    }
}
