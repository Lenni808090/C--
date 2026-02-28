class CompiledFunction {
    public byte[] bytecode;
    public Value[] constants;
    public int LocalCount;

    public CompiledFunction(byte[] bytecode, Value[] constants, int localCount) {
        this.bytecode = bytecode;
        this.constants = constants;
        LocalCount = localCount;
    }
}