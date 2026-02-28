using System.ComponentModel;

class VM {
    Value[] regs;
    Value[] constants;

    Value[] locals;
    byte[] bytecode;

    public VM(Value[] regs, Value[] constants, int localCount, byte[] bytecode) {
        this.regs = regs;
        this.constants = constants;
        locals = new Value[localCount];
        this.bytecode = bytecode;
    }

    int instructionPointer;

    public Value Run() {
        instructionPointer = 0;
        while (true) {
            OpCode currentByteCode = (OpCode)bytecode[instructionPointer++];


            switch (currentByteCode) {
                case OpCode.LOAD_CONST: {
                        byte dstReg = bytecode[instructionPointer++];
                        byte constIndex = bytecode[instructionPointer++];
                        regs[dstReg] = constants[constIndex];
                        break;
                    }

                case OpCode.STORE_LOCAL: {
                        byte srcReg = bytecode[instructionPointer++];
                        byte localIndex = bytecode[instructionPointer++];
                        locals[localIndex] = regs[srcReg];
                        break;
                    }

                case OpCode.LOAD_LOCAL: {
                        byte dstReg = bytecode[instructionPointer++];
                        byte localIndex = bytecode[instructionPointer++];
                        regs[dstReg] = locals[localIndex];
                        break;
                    }

                case OpCode.RETURN: {
                        byte returnReg = bytecode[instructionPointer++];
                        return regs[returnReg];
                    }

                case OpCode.ADD_INT: {
                        byte dstReg = bytecode[instructionPointer++];
                        byte leftReg = bytecode[instructionPointer++];
                        byte rightReg = bytecode[instructionPointer++];
                        int left = (int)regs[leftReg].RawData;
                        int right = (int)regs[rightReg].RawData;
                        regs[dstReg] = new Value(ValueType.Int, left + right);
                        break;
                    }

                case OpCode.JUMP: {
                        int offset = BitConverter.ToInt32(bytecode, instructionPointer);
                        instructionPointer += 4;
                        instructionPointer += offset;
                        break;
                    }

                case OpCode.JUMP_IF_FALSE: {
                        byte condReg = bytecode[instructionPointer++];
                        bool cond = regs[condReg].AsBool();
                        int offset = BitConverter.ToInt32(bytecode, instructionPointer);
                        instructionPointer += 4;
                        if (!cond) {
                            instructionPointer += offset;
                        }
                        break;
                    }

                case OpCode.JUMP_IF_TRUE: {
                        byte condReg = bytecode[instructionPointer++];
                        bool cond = regs[condReg].AsBool();
                        int offset = BitConverter.ToInt32(bytecode, instructionPointer);
                        instructionPointer += 4;
                        if (cond) {
                            instructionPointer += offset;
                        }
                        break;
                    }

                case OpCode.CMP_EQ_INT: {
                        byte dstReg = bytecode[instructionPointer++];
                        byte leftReg = bytecode[instructionPointer++];
                        byte rightReg = bytecode[instructionPointer++];
                        int result = ((int)regs[leftReg].RawData == (int)regs[rightReg].RawData) ? 1 : 0;
                        regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }

                case OpCode.CMP_LT_INT: {
                        byte dstReg = bytecode[instructionPointer++];
                        byte leftReg = bytecode[instructionPointer++];
                        byte rightReg = bytecode[instructionPointer++];
                        int result = ((int)regs[leftReg].RawData < (int)regs[rightReg].RawData) ? 1 : 0;
                        regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }

                case OpCode.CMP_MT_INT: {
                        byte dstReg = bytecode[instructionPointer++];
                        byte leftReg = bytecode[instructionPointer++];
                        byte rightReg = bytecode[instructionPointer++];
                        int result = ((int)regs[leftReg].RawData > (int)regs[rightReg].RawData) ? 1 : 0;
                        regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }
                default:
                    throw new InvalidOperationException("Unknown opcode at position " + (instructionPointer - 1));

            }
        }
    }


}