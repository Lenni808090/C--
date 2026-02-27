using System.ComponentModel;

class VM {
    Value[] regs;
    Value[] constants;
    byte[] bytecode;

    public VM(Value[] regs, Value[] constants, byte[] bytecode) {
        this.regs = regs;
        this.constants = constants;
        this.bytecode = bytecode;
    }

    int instructionPointer;

    public Value Run() {
        instructionPointer = 0;
        while (true) {
            OpCode currentByteCode = (OpCode)bytecode[instructionPointer++];


            switch (currentByteCode) {
                case OpCode.LOAD_CONST: {
                        byte a = bytecode[instructionPointer++];
                        byte b = bytecode[instructionPointer++];
                        regs[a] = constants[b];
                        break;
                    }
                case OpCode.RETURN: {
                        byte a = bytecode[instructionPointer++];
                        Value returnValue = regs[a];
                        return returnValue;
                    }
                case OpCode.ADD_INT: {
                        byte a = bytecode[instructionPointer++];
                        byte b = bytecode[instructionPointer++];
                        byte c = bytecode[instructionPointer++];
                        int intA = (int)regs[b].RawData;
                        int intB = (int)regs[c].RawData;
                        int res = intA + intB;
                        regs[a] = new Value(ValueType.Int, res);
                        break;
                    }
                case OpCode.JUMP: {
                        int offset = BitConverter.ToInt32(bytecode, instructionPointer);
                        instructionPointer += 4;
                        instructionPointer += offset;
                        break;
                    }
                case OpCode.JUMP_IF_FALSE: {
                        byte a = bytecode[instructionPointer++];
                        bool boolA = regs[a].AsBool();
                        int offset = BitConverter.ToInt32(bytecode, instructionPointer);
                        instructionPointer += 4;
                        if (!boolA) {
                            instructionPointer += offset;
                        }
                        break;
                    }
                case OpCode.JUMP_IF_TRUE: {
                        byte a = bytecode[instructionPointer++];
                        bool boolA = regs[a].AsBool();
                        int offset = BitConverter.ToInt32(bytecode, instructionPointer);
                        instructionPointer += 4;
                        if (boolA) {
                            instructionPointer += offset;
                        }
                        break;
                    }
                case OpCode.CMP_EQ_INT: {
                        byte a = bytecode[instructionPointer++];
                        byte b = bytecode[instructionPointer++];
                        byte c = bytecode[instructionPointer++];
                        int boolValue = ((int)regs[b].RawData == (int)regs[c].RawData) ? 1 : 0;
                        regs[a] = new Value(ValueType.Bool, boolValue);
                        break;
                    }
                case OpCode.CMP_LT_INT: {
                        byte a = bytecode[instructionPointer++];
                        byte b = bytecode[instructionPointer++];
                        byte c = bytecode[instructionPointer++];
                        int boolValue = ((int)regs[b].RawData < (int)regs[c].RawData) ? 1 : 0;
                        regs[a] = new Value(ValueType.Bool, boolValue);
                        break;
                    }
                case OpCode.CMP_MT_INT: {
                        byte a = bytecode[instructionPointer++];
                        byte b = bytecode[instructionPointer++];
                        byte c = bytecode[instructionPointer++];
                        int boolValue = ((int)regs[b].RawData > (int)regs[c].RawData) ? 1 : 0;
                        regs[a] = new Value(ValueType.Bool, boolValue);
                        break;
                    }
                default:
                    throw new InvalidOperationException("Unknown opcode at position " + (instructionPointer - 1));

            }
        }
    }


}