using System.ComponentModel;
using CMinus.CodeGen;

namespace CMinus.Runtime;

class VM {
    Value[] constants;

    CallFrame currentFrame => callFrames.Peek();
    CompiledFunction currentFunction => functions[currentFrame.functionInd];

    byte[] currentBytecode => currentFunction.bytecode;
    CompiledFunction[] functions;
    int entryInd;
    Stack<CallFrame> callFrames;

    public VM(CompiledProgram compiledProgram, Value[] constants) {
        functions = compiledProgram.compiledFunctions;
        entryInd = compiledProgram.entryFuncInd;
        callFrames = new();
        this.constants = constants;
        PushEntryFrame();
    }

    void PushEntryFrame() {
        var entry = functions[entryInd];
        var entryCallFrame = entry.AsCallFrame(null, entryInd);
        callFrames.Push(entryCallFrame);
    }
    public Value Run() {
        while (true) {
            var frame = currentFrame;
            OpCode currentByteCode = (OpCode)currentBytecode[frame.instructionPointer++];


            switch (currentByteCode) {
                case OpCode.LOAD_CONST: {
                        ushort dstReg = getu16();
                        ushort constIndex = getu16();
                        frame.regs[dstReg] = constants[constIndex];
                        break;
                    }

                case OpCode.STORE_LOCAL: {
                        ushort srcReg = getu16();
                        ushort localIndex = getu16();
                        frame.locals[localIndex] = frame.regs[srcReg];
                        break;
                    }
                case OpCode.LOAD_LOCAL: {
                        ushort dstReg = getu16();
                        ushort localIndex = getu16();
                        frame.regs[dstReg] = frame.locals[localIndex];
                        break;
                    }


                case OpCode.RETURN: {
                        ushort srcReg = getu16();
                        Value result = frame.regs[srcReg];


                        var returnedCallFrame = callFrames.Pop();

                        if (returnedCallFrame.returnReg is null) {
                            return result;
                        }

                        var callerFrame = callFrames.Peek();


                        callerFrame.regs[returnedCallFrame.returnReg.Value] = result;
                        break;
                    }


                case OpCode.ADD_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int left = (int)frame.regs[leftReg].RawData;
                        int right = (int)frame.regs[rightReg].RawData;
                        frame.regs[dstReg] = new Value(ValueType.Int, left + right);
                        break;
                    }
                case OpCode.SUBTRACT_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int left = (int)frame.regs[leftReg].RawData;
                        int right = (int)frame.regs[rightReg].RawData;
                        frame.regs[dstReg] = new Value(ValueType.Int, left - right);
                        break;
                    }
                case OpCode.MULTIPLY_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int left = (int)frame.regs[leftReg].RawData;
                        int right = (int)frame.regs[rightReg].RawData;
                        frame.regs[dstReg] = new Value(ValueType.Int, left * right);
                        break;
                    }
                case OpCode.DIVIDE_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int left = (int)frame.regs[leftReg].RawData;
                        int right = (int)frame.regs[rightReg].RawData;
                        frame.regs[dstReg] = new Value(ValueType.Int, left / right);
                        break;
                    }
                case OpCode.NEG_INT: {
                        ushort dstReg = getu16();
                        ushort srcReg = getu16();

                        int toBeNegged = (int)frame.regs[srcReg].RawData;
                        frame.regs[dstReg] = new Value(ValueType.Int, -toBeNegged);
                        break;
                    }

                case OpCode.NOT: {
                        ushort dstReg = getu16();
                        ushort srcReg = getu16();

                        frame.regs[dstReg] = new Value(ValueType.Bool, frame.regs[srcReg].AsBool() ? 0 : 1);
                        break;
                    }

                case OpCode.JUMP: {
                        int offset = BitConverter.ToInt32(currentBytecode, frame.instructionPointer);
                        frame.instructionPointer += 4;
                        frame.instructionPointer += offset;
                        break;
                    }
                case OpCode.JUMP_IF_FALSE: {
                        ushort condReg = getu16();
                        bool cond = frame.regs[condReg].AsBool();
                        int offset = BitConverter.ToInt32(currentBytecode, frame.instructionPointer);
                        frame.instructionPointer += 4;
                        if (!cond) {
                            frame.instructionPointer += offset;
                        }
                        break;
                    }

                case OpCode.JUMP_IF_TRUE: {
                        ushort condReg = getu16();
                        bool cond = frame.regs[condReg].AsBool();
                        int offset = BitConverter.ToInt32(currentBytecode, frame.instructionPointer);
                        frame.instructionPointer += 4;
                        if (cond) {
                            frame.instructionPointer += offset;
                        }
                        break;
                    }

                case OpCode.CMP_EQ: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int result = ((int)frame.regs[leftReg].RawData == (int)frame.regs[rightReg].RawData) ? 1 : 0;
                        frame.regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }
                case OpCode.CMP_LT_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int result = ((int)frame.regs[leftReg].RawData < (int)frame.regs[rightReg].RawData) ? 1 : 0;
                        frame.regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }
                case OpCode.CMP_MT_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int result = ((int)frame.regs[leftReg].RawData > (int)frame.regs[rightReg].RawData) ? 1 : 0;
                        frame.regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }
                case OpCode.CMP_LTE_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int result = ((int)frame.regs[leftReg].RawData <= (int)frame.regs[rightReg].RawData) ? 1 : 0;
                        frame.regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }
                case OpCode.CMP_MTE_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int result = ((int)frame.regs[leftReg].RawData >= (int)frame.regs[rightReg].RawData) ? 1 : 0;
                        frame.regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }
                case OpCode.CMP_NEQ: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int result = ((int)frame.regs[leftReg].RawData != (int)frame.regs[rightReg].RawData) ? 1 : 0;
                        frame.regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }


                case OpCode.MOVE: {
                        ushort dstReg = getu16();
                        ushort srcReg = getu16();
                        frame.regs[dstReg] = frame.regs[srcReg];
                        break;
                    }


                case OpCode.CALL: {
                        ushort dstReg = getu16();
                        int functionIndex = BitConverter.ToInt32(currentBytecode, frame.instructionPointer);
                        frame.instructionPointer += 4;
                        ushort argCount = getu16();
                        ushort[] argRegs = new ushort[argCount];
                        for (int i = 0; i < argCount; i++) {
                            argRegs[i] = getu16();
                        }

                        CallFunction(dstReg, functionIndex, argRegs.ToArray());
                        break;
                    }

                default:
                    throw new InvalidOperationException("Unknown opcode at position " + (currentFrame.instructionPointer - 1));

            }
        }
    }

    void CallFunction(ushort dstReg, int functionIndex, ushort[] argRegs) {
        var callFrame = functions[functionIndex].AsCallFrame(dstReg, functionIndex);
        var callerFrame = callFrames.Peek();

        for (int i = 0; i < argRegs.Length; i++) {
            callFrame.locals[i] = callerFrame.regs[argRegs[i]];
        }

        callFrames.Push(callFrame);
    }

    ushort getu16() {
        ushort reg = BitConverter.ToUInt16(currentBytecode, currentFrame.instructionPointer);
        currentFrame.instructionPointer += 2;
        return reg;
    }

}
