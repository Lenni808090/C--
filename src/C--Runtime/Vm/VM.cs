using System.ComponentModel;
using CMinus.CodeGen;

namespace CMinus.Runtime;

class VM {
    Value[] constants;
    RuntimeTypeDesc[] typeTable;
    Heap heap;
    CallFrame currentFrame => callFrames.Peek();
    CompiledFunction currentFunction => functions[currentFrame.functionInd];

    byte[] currentBytecode => currentFunction.bytecode;
    CompiledFunction[] functions;
    int entryInd;
    Stack<CallFrame> callFrames;



    public VM(CompiledProgram compiledProgram) {
        functions = compiledProgram.compiledFunctions;
        entryInd = compiledProgram.entryFuncInd;
        typeTable = compiledProgram.typeTable;
        heap = new();
        callFrames = new();
        this.constants = compiledProgram.constants;
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
            OpCode currentOpCode = (OpCode)currentBytecode[frame.instructionPointer++];


            switch (currentOpCode) {
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
                        int left = frame.regs[leftReg].AsInt();
                        int right = frame.regs[rightReg].AsInt();
                        frame.regs[dstReg] = new Value(ValueType.Int, left + right);
                        break;
                    }
                case OpCode.SUBTRACT_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int left = frame.regs[leftReg].AsInt();
                        int right = frame.regs[rightReg].AsInt();
                        frame.regs[dstReg] = new Value(ValueType.Int, left - right);
                        break;
                    }
                case OpCode.MULTIPLY_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int left = frame.regs[leftReg].AsInt();
                        int right = frame.regs[rightReg].AsInt();
                        frame.regs[dstReg] = new Value(ValueType.Int, left * right);
                        break;
                    }
                case OpCode.DIVIDE_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int left = frame.regs[leftReg].AsInt();
                        int right = frame.regs[rightReg].AsInt();
                        frame.regs[dstReg] = new Value(ValueType.Int, left / right);
                        break;
                    }
                case OpCode.NEG_INT: {
                        ushort dstReg = getu16();
                        ushort srcReg = getu16();

                        int toBeNegged = frame.regs[srcReg].AsInt();
                        frame.regs[dstReg] = new Value(ValueType.Int, -toBeNegged);
                        break;
                    }
                case OpCode.MODULUS_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int left = frame.regs[leftReg].AsInt();
                        int right = frame.regs[rightReg].AsInt();
                        frame.regs[dstReg] = new Value(ValueType.Int, left % right);
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
                        int result = (frame.regs[leftReg].Type == frame.regs[rightReg].Type && frame.regs[leftReg].RawData == frame.regs[rightReg].RawData) ? 1 : 0;
                        frame.regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }
                case OpCode.CMP_LT_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int result = (frame.regs[leftReg].AsInt() < frame.regs[rightReg].AsInt()) ? 1 : 0;
                        frame.regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }
                case OpCode.CMP_MT_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int result = (frame.regs[leftReg].AsInt() > frame.regs[rightReg].AsInt()) ? 1 : 0;
                        frame.regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }
                case OpCode.CMP_LTE_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int result = (frame.regs[leftReg].AsInt() <= frame.regs[rightReg].AsInt()) ? 1 : 0;
                        frame.regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }
                case OpCode.CMP_MTE_INT: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int result = (frame.regs[leftReg].AsInt() >= frame.regs[rightReg].AsInt()) ? 1 : 0;
                        frame.regs[dstReg] = new Value(ValueType.Bool, result);
                        break;
                    }
                case OpCode.CMP_NEQ: {
                        ushort dstReg = getu16();
                        ushort leftReg = getu16();
                        ushort rightReg = getu16();
                        int result = (frame.regs[leftReg].Type != frame.regs[rightReg].Type || frame.regs[leftReg].RawData != frame.regs[rightReg].RawData) ? 1 : 0;
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

                case OpCode.NEW_ARRAY: {
                        ushort dstReg = getu16();
                        int typeId = GetI32();
                        ushort lengthReg = getu16();
                        int length = frame.regs[lengthReg].AsInt();
                        if (length < 0) {
                            throw new Exception("array length must be positive");
                        }
                        AllocateNewArray(dstReg, typeId, length);
                        break;
                    }
                case OpCode.ARRAY_LENGTH: {
                        ushort dstReg = getu16();
                        ushort arrayReg = getu16();

                        ArrayObject arrayObj = GetArrayObject(arrayReg);

                        frame.regs[dstReg] = new Value(ValueType.Int, arrayObj.Length);

                        break;
                    }
                case OpCode.STORE_ELEMENT: {
                        ushort srcReg = getu16();
                        ushort arrayReg = getu16();
                        ushort indexReg = getu16();

                        int index = frame.regs[indexReg].AsInt();

                        ArrayObject arrayObj = GetArrayObject(arrayReg);
                        if (index < 0 || index >= arrayObj.Length) {
                            throw new Exception("Index out of range");
                        }
                        arrayObj.Elements[index] = frame.regs[srcReg];

                        break;
                    }
                case OpCode.LOAD_ELEMNT: {
                        ushort dstReg = getu16();
                        ushort arrayReg = getu16();
                        ushort indexReg = getu16();

                        int index = frame.regs[indexReg].AsInt();

                        ArrayObject arrayObj = GetArrayObject(arrayReg);
                        if (index < 0 || index >= arrayObj.Length) {
                            throw new Exception("Index out of range");
                        }
                        frame.regs[dstReg] = arrayObj.Elements[index];
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

        //possible because binder adds params first as locals
        for (int i = 0; i < argRegs.Length; i++) {
            callFrame.locals[i] = callerFrame.regs[argRegs[i]];
        }

        callFrames.Push(callFrame);
    }

    ArrayObject GetArrayObject(ushort arrayReg) {
        var frame = callFrames.Peek();
        Value arrayValue = frame.regs[arrayReg];

        if (arrayValue.Type == ValueType.Null) {
            throw new NullReferenceException("array reference is null");
        }

        int heapRefId = arrayValue.AsHeapRef();
        HeapObject heapObject = heap.GetHeapObject(heapRefId);
        if (heapObject is not ArrayObject arrayObj) {
            throw new Exception("heap reference does not point to an array");
        }
        return arrayObj;
    }

    void AllocateNewArray(ushort dstReg, int typeId, int length) {
        RuntimeTypeDesc arrayType = GetTypeDesc(typeId);
        if (arrayType.Kind != RuntimeTypeKind.Array || arrayType.ElementTypeId is null) {
            throw new Exception("NEW_ARRAY requires an array type descriptor");
        }

        Value[] Elements = new Value[length];
        SetArrayDefaultValues(Elements, arrayType.ElementTypeId.Value);

        ArrayObject array = new ArrayObject(typeId, arrayType.ElementTypeId.Value, Elements);
        int heapObjId = heap.Allocate(array);
        var callFrame = callFrames.Peek();

        var heapObjRef = new Value(ValueType.HeapRef, heapObjId);
        callFrame.regs[dstReg] = heapObjRef;
    }

    void SetArrayDefaultValues(Value[] elements, int elementTypeId) {
        Value defaultValue = DefaultValueForType(elementTypeId);

        for (int i = 0; i < elements.Length; i++) {
            elements[i] = defaultValue;
        }
    }

    Value DefaultValueForType(int typeId) {
        RuntimeTypeDesc type = GetTypeDesc(typeId);
        return type.Kind switch {
            RuntimeTypeKind.Int => new Value(ValueType.Int, 0),
            RuntimeTypeKind.Bool => new Value(ValueType.Bool, 0),
            RuntimeTypeKind.Char => new Value(ValueType.Char, 0),
            RuntimeTypeKind.Array => new Value(ValueType.Null, 0),
            RuntimeTypeKind.Object => new Value(ValueType.Null, 0),
            _ => throw new Exception("unknown runtime type kind"),
        };
    }

    RuntimeTypeDesc GetTypeDesc(int typeId) {
        if (typeId < 0 || typeId >= typeTable.Length) {
            throw new Exception("unknown runtime type id " + typeId);
        }

        return typeTable[typeId];
    }


    ushort getu16() {
        ushort reg = BitConverter.ToUInt16(currentBytecode, currentFrame.instructionPointer);
        currentFrame.instructionPointer += 2;
        return reg;
    }

    int GetI32() {
        int value = BitConverter.ToInt32(currentBytecode, currentFrame.instructionPointer);
        currentFrame.instructionPointer += 4;
        return value;
    }

}
