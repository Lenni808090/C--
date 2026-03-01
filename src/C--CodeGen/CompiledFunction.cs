using CMinus.Runtime;

namespace CMinus.CodeGen;

class CompiledFunction {
    public byte[] bytecode;
    public Value[] constants;
    public int localCount;

    public int maxRegCount;

    public CompiledFunction(byte[] bytecode, Value[] constants, int localCount, int maxRegCount) {
        this.bytecode = bytecode;
        this.constants = constants;
        this.localCount = localCount;
        this.maxRegCount = maxRegCount;
    }
}
