using CMinus.Runtime;
using CMinus.Compiler;

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
    public InstructionDebugInfo[] debugInfo;

    public CompiledFunction(byte[] bytecode, int localCount, int maxRegCount, int paramCount, InstructionDebugInfo[] debugInfo) {
        this.bytecode = bytecode;
        this.paramCount = paramCount;
        this.localCount = localCount;
        this.maxRegCount = maxRegCount;
        this.debugInfo = debugInfo;
    }

    public CallFrame AsCallFrame(ushort? returnReg, int functionInd) {
        Value[] regs = new Value[maxRegCount];
        Value[] locals = new Value[localCount + paramCount];
        return new CallFrame(regs, locals, returnReg, functionInd);
    }
}

readonly struct InstructionDebugInfo {
    public int BytecodeOffset { get; }
    public SourceLocation Location { get; }

    public InstructionDebugInfo(int bytecodeOffset, SourceLocation location) {
        BytecodeOffset = bytecodeOffset;
        Location = location;
    }
}

