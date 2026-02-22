using System.ComponentModel;

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
                case OpCode.ADD_INT:
                    {
                        byte a = bytecode[instructionPointer++];
                        byte b = bytecode[instructionPointer++];
                        byte c = bytecode[instructionPointer++];
                        int intA = (int)regs[b].RawData;
                        int intB = (int)regs[c].RawData;
                        int res = intA + intB;
                        regs[a] = new Value(ValueType.Int, res);
                        break;
                    }
                case OpCode.JUMP:
                    {
                        int offset = BitConverter.ToInt32(bytecode, instructionPointer);
                        instructionPointer += 4;
                        instructionPointer += offset;
                        break;
                    }
                default:
                    throw new InvalidOperationException("Unknown opcode at position " + (instructionPointer - 1));

            }
        }
    }


}