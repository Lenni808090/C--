using System.ComponentModel;
using CMinus.Runtime;

namespace CMinus.CodeGen;

class Emitter {
    List<byte> emittedBytecode;
    List<Fixup> fixups;
    Dictionary<Label, int> labelPos;
    public int pos => emittedBytecode.Count;

    public Emitter() {
        fixups = new();
        labelPos = new();
        emittedBytecode = new();
    }

    public Label NewLabel() {
        return new Label();
    }

    public void EmitJump(Label label) {
        emittedBytecode.Add((byte)OpCode.JUMP);
        int patchPos = pos;
        EmitI32(0);
        fixups.Add(new Fixup(patchPos, label));
    }

    public void EmitJumpIfFalse(ushort condReg, Label label) {
        EmitOp(OpCode.JUMP_IF_FALSE);
        EmitU16(condReg);
        int patchPos = pos;
        EmitI32(0);
        fixups.Add(new Fixup(patchPos, label));
    }


    public void EmitJumpIfTrue(ushort firstIndex, Label label) {
        EmitOp(OpCode.JUMP_IF_TRUE);
        EmitU16(firstIndex);
        int patchPos = pos;
        EmitI32(0);
        fixups.Add(new Fixup(patchPos, label));
    }
    public void DefineLabel(Label label) {
        if (labelPos.TryGetValue(label, out var _)) {
            throw new Exception("label already existing");
        }
        labelPos.Add(label, pos);
    }
    public void EmitI32(int value) {
        var bytes = BitConverter.GetBytes(value);
        emittedBytecode.AddRange(bytes);
    }
    public void EmitU16(ushort value) {
        byte[] conValue = BitConverter.GetBytes(value);
        emittedBytecode.AddRange(conValue);
    }
    public void EmitCall(ushort dstReg, int functionIndex, ushort argCount, ushort[] argRegs) {
        EmitOp(OpCode.CALL);
        EmitU16(dstReg);
        EmitI32(functionIndex);
        EmitU16(argCount);
        foreach (ushort argReg in argRegs) {
            EmitU16(argReg);
        }
    }
    public void EmitLoadConstant(ushort dstReg, ushort constIndex) {
        EmitOp(OpCode.LOAD_CONST);
        EmitU16(dstReg);
        EmitU16(constIndex);
    }

    public void EmitStoreLocal(ushort srcReg, ushort localIndex) {
        EmitOp(OpCode.STORE_LOCAL);
        EmitU16(srcReg);
        EmitU16(localIndex);
    }

    public void EmitLoadLocal(ushort dstReg, ushort localIndex) {
        EmitOp(OpCode.LOAD_LOCAL);
        EmitU16(dstReg);
        EmitU16(localIndex);
    }

    public void EmitNewArray(ushort dstReg, int typeId, ushort lengthReg) {
        EmitOp(OpCode.NEW_ARRAY);
        EmitU16(dstReg);
        EmitI32(typeId);
        EmitU16(lengthReg);
    }

    public void EmitStoreElement(ushort srcReg, ushort arrayReg, ushort indexReg) {
        EmitOp(OpCode.STORE_ELEMENT);
        EmitU16(srcReg);
        EmitU16(arrayReg);
        EmitU16(indexReg);
    }

    public void EmitLoadElement(ushort dstReg, ushort arrayReg, ushort indexReg) {
        EmitOp(OpCode.LOAD_ELEMNT);
        EmitU16(dstReg);
        EmitU16(arrayReg);
        EmitU16(indexReg);
    }

    public void EmitArrayLength(ushort dstReg, ushort arrayReg) {
        EmitOp(OpCode.ARRAY_LENGTH);
        EmitU16(dstReg);
        EmitU16(arrayReg);
    }

    public void EmitReturn(ushort returnReg) {
        EmitOp(OpCode.RETURN);
        EmitU16(returnReg);
    }

    private void EmitRRR(OpCode op, ushort dstReg, ushort leftReg, ushort rightReg) {
        EmitOp(op);
        EmitU16(dstReg);
        EmitU16(leftReg);
        EmitU16(rightReg);
    }

    public void EmitAddInt(ushort dstReg, ushort leftReg, ushort rightReg) {
        EmitRRR(OpCode.ADD_INT, dstReg, leftReg, rightReg);
    }

    public void EmitSubtractInt(ushort dstReg, ushort leftReg, ushort rightReg) {
        EmitRRR(OpCode.SUBTRACT_INT, dstReg, leftReg, rightReg);
    }

    public void EmitMultiplyInt(ushort dstReg, ushort leftReg, ushort rightReg) {
        EmitRRR(OpCode.MULTIPLY_INT, dstReg, leftReg, rightReg);
    }

    public void EmitDivideInt(ushort dstReg, ushort leftReg, ushort rightReg) {
        EmitRRR(OpCode.DIVIDE_INT, dstReg, leftReg, rightReg);
    }
    public void EmitModulusInt(ushort dstReg, ushort leftReg, ushort rightReg) {
        EmitRRR(OpCode.MODULUS_INT, dstReg, leftReg, rightReg);
    }
    public void EmitNegInt(ushort dstReg, ushort srcReg) {
        EmitOp(OpCode.NEG_INT);
        EmitU16(dstReg);
        EmitU16(srcReg);
    }

    public void EmitNot(ushort dstReg, ushort srcReg) {
        EmitOp(OpCode.NOT);
        EmitU16(dstReg);
        EmitU16(srcReg);
    }
    public void EmitCmpLTInt(ushort dstReg, ushort leftReg, ushort rightReg) {
        EmitRRR(OpCode.CMP_LT_INT, dstReg, leftReg, rightReg);
    }

    public void EmitCmpLTEInt(ushort dstReg, ushort leftReg, ushort rightReg) {
        EmitRRR(OpCode.CMP_LTE_INT, dstReg, leftReg, rightReg);
    }

    public void EmitCmpMTInt(ushort dstReg, ushort leftReg, ushort rightReg) {
        EmitRRR(OpCode.CMP_MT_INT, dstReg, leftReg, rightReg);
    }

    public void EmitCmpMTEInt(ushort dstReg, ushort leftReg, ushort rightReg) {
        EmitRRR(OpCode.CMP_MTE_INT, dstReg, leftReg, rightReg);
    }
    public void EmitCmpEQ(ushort dstReg, ushort leftReg, ushort rightReg) {
        EmitRRR(OpCode.CMP_EQ, dstReg, leftReg, rightReg);
    }
    public void EmitCmpNEQ(ushort dstReg, ushort leftReg, ushort rightReg) {
        EmitRRR(OpCode.CMP_NEQ, dstReg, leftReg, rightReg);
    }


    public void EmitOp(OpCode opCode) {
        emittedBytecode.Add((byte)opCode);
    }

    public void PatchAll() {
        foreach (Fixup fixup in fixups) {
            if (labelPos.TryGetValue(fixup.Lable, out int foundPos)) {
                // plus 4 because the jump starts after the 4 bytes for offset are read
                int offset = foundPos - (fixup.PatchPos + 4);
                var bytes = BitConverter.GetBytes(offset);
                for (int i = 0; i < 4; i++) {
                    emittedBytecode[i + fixup.PatchPos] = bytes[i];
                }
            }
            else {
                throw new Exception("label does not exist");
            }
        }
    }

    public byte[] BytecodeToArray() {
        return emittedBytecode.ToArray();
    }

    public void Reset() {
        emittedBytecode.Clear();
        fixups.Clear();
        labelPos.Clear();
    }
}
