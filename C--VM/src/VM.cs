class VM
{
    Value[] regs;
    Value[] constants;
    byte[] bytecode;

    int instructionPointer;

    public void Run()
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
                        break;
                    }
                default:
                    throw new InvalidOperationException("Unknown opcode at position " + (instructionPointer - 1));

            }
        }
    }


}