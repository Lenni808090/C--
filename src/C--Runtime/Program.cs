class Program {
    static void Main() {
        Value[] regs = new Value[3];
        Value[] constants = new Value[2];
        byte[] bytecode = new byte[12];
        bytecode[0] = (byte)OpCode.LOAD_CONST;
        bytecode[1] = 0;
        bytecode[2] = 0;
        bytecode[3] = (byte)OpCode.LOAD_CONST;
        bytecode[4] = 1;
        bytecode[5] = 1;
        bytecode[6] = (byte)OpCode.ADD_INT;
        bytecode[7] = 2;
        bytecode[8] = 0;
        bytecode[9] = 1;
        bytecode[10] = (byte)OpCode.RETURN;
        bytecode[11] = 2;
        constants[0] = new Value(ValueType.Int, 5);
        constants[1] = new Value(ValueType.Int, 5);
        VM vm = new VM(regs, constants, bytecode);
        Value result = vm.Run();
        Console.WriteLine(result.RawData);
    }
}