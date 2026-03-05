using System.ComponentModel;

namespace CMinus.Runtime;

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
                        ushort dstReg = getu16();
                        ushort constIndex = getu16();
                        regs[dstReg] = constants[constIndex];
                        break;
                    }

                case OpCode.STORE_LOCAL: {
                        ushort srcReg = getu16();
                        ushort localIndex = getu16();
                        locals[localIndex] = regs[srcReg];
                        break;
                    }
                case OpCode.LOAD_LOCAL: {
                        ushort dstReg = getu16();
                        ushort localIndex = getu16();
                        regs[dstReg] = locals[localIndex];
                        break;
                    }


                case OpCode.RETURN: {
                        ushort returnReg = getu16();
                        return regs[returnReg];
                    }


                case OpCode.ADD_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int left = (int)regs[leftReg].RawData;
                        int right = (int)regs[rightReg].RawData;
                        regs[dstReg] = new Value(ValueType.Int, left + right);
                        break;
                    }
                case OpCode.SUBTRACT_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int left = (int)regs[leftReg].RawData;
                        int right = (int)regs[rightReg].RawData;
                        regs[dstReg] = new Value(ValueType.Int, left - right);
                        break;
                    }
                case OpCode.MULTIPLY_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int left = (int)regs[leftReg].RawData;
                        int right = (int)regs[rightReg].RawData;
                        regs[dstReg] = new Value(ValueType.Int, left * right);
                        break;
                    }
                case OpCode.DIVIDE_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int left = (int)regs[leftReg].RawData;
                        int right = (int)regs[rightReg].RawData;
                        regs[dstReg] = new Value(ValueType.Int, left / right);
                        break;
                    }
                case OpCode.NEG_INT: {
                        ushort dstReg = getu16();
                        ushort srcReg = getu16();

                        int toBeNegged = (int)regs[srcReg].RawData;
                        regs[dstReg] = new Value(ValueType.Int, -toBeNegged);
                        break;
                    }

                case OpCode.NOT: {
                        ushort dstReg = getu16();
                        ushort srcReg = getu16();

                        regs[dstReg] = new Value(ValueType.Bool, regs[srcReg].AsBool() ? 0 : 1);
                        break;
                    }

                case OpCode.JUMP: {
                        int offset = BitConverter.ToInt32(bytecode, instructionPointer);
                        instructionPointer += 4;
                        instructionPointer += offset;
                        break;
                    }
                case OpCode.JUMP_IF_FALSE: {
                        ushort condReg = getu16();
                        bool cond = regs[condReg].AsBool();
                        int offset = BitConverter.ToInt32(bytecode, instructionPointer);
                        instructionPointer += 4;
                        if (!cond) {
                            instructionPointer += offset;
                        }
                        break;
                    }

                case OpCode.JUMP_IF_TRUE: {
                        ushort condReg = getu16();
                        bool cond = regs[condReg].AsBool();
                        int offset = BitConverter.ToInt32(bytecode, instructionPointer);
                        instructionPointer += 4;
                        if (cond) {
                            instructionPointer += offset;
                        }
                        break;
                    }

                case OpCode.CMP_EQ: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int result = ((int)regs[leftReg].RawData == (int)regs[rightReg].RawData) ? 1 : 0;
                        regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }
                case OpCode.CMP_LT_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int result = ((int)regs[leftReg].RawData < (int)regs[rightReg].RawData) ? 1 : 0;
                        regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }
                case OpCode.CMP_MT_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int result = ((int)regs[leftReg].RawData > (int)regs[rightReg].RawData) ? 1 : 0;
                        regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }
                case OpCode.CMP_LTE_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int result = ((int)regs[leftReg].RawData <= (int)regs[rightReg].RawData) ? 1 : 0;
                        regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }
                case OpCode.CMP_MTE_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int result = ((int)regs[leftReg].RawData >= (int)regs[rightReg].RawData) ? 1 : 0;
                        regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }
                case OpCode.CMP_NEQ: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int result = ((int)regs[leftReg].RawData != (int)regs[rightReg].RawData) ? 1 : 0;
                        regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }


                case OpCode.MOVE: {
                        ushort dstReg = getu16();
                        ushort srcReg = getu16();
                        regs[dstReg] = regs[srcReg];
                        break;
                    }

                default:
                    throw new InvalidOperationException("Unknown opcode at position " + (instructionPointer - 1));

            }
        }
    }

    ushort getu16() {
        ushort reg = BitConverter.ToUInt16(bytecode, instructionPointer);
        instructionPointer += 2;
        return reg;
    }

}
