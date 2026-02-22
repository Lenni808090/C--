class VM
{
    Value[] regs;
    Value[] constants;
    byte[] bytecode;

    public VM(Value[] regs, Value[] constants, byte[] bytecode)
    {
        this.regs = regs;
        this.constants = constants;
        this.bytecode = bytecode;
    }

    int instructionPointer;

    public Value Run()
    {
        while (true)
        {
            OpCode currentByteCode = (OpCode)bytecode[instructionPointer++];


            switch (currentByteCode)
            {
                case OpCode.LOAD_CONST:
                    {
                        byte a = bytecode[instructionPointer++];
                        byte b = bytecode[instructionPointer++];
                        regs[a] = constants[b];
                        break;
                    }
                case OpCode.RETURN:
                    {
                        byte a = bytecode[instructionPointer++];
                        Value returnValue = regs[a];
                        return returnValue;
                    }
                default:
                    throw new InvalidOperationException("Unknown opcode at position " + (instructionPointer - 1));

            }
        }
    }


}