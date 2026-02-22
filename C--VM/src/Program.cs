class Program
{
    static void Main()
    {
        Value[] regs = new Value[1];
        Value[] constants = new Value[1];
        byte[] bytecode = new byte[5];
        bytecode[0] = (byte)OpCode.LOAD_CONST;
        bytecode[1] = 0;
        bytecode[2] = 0;
        bytecode[3] = (byte)OpCode.RETURN;
        bytecode[4] = 0;
        constants[0] = new Value
        {
            valueType = ValueType.Int,
            RawData = 5
        };
        VM vm = new VM(regs, constants, bytecode);
        Value result = vm.Run();
        Console.WriteLine(result.RawData);
    }
}