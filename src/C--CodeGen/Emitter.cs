using System.ComponentModel;
using CMinus.Runtime;

namespace CMinus.CodeGen;

class Emitter {
    List<byte> emittedBytecode;
    List<Fixup> fixups;
    Dictionary<Label, int> labelPos;
    int pos => emittedBytecode.Count;
    public int Position => pos;

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

    public void EmitJumpIfFalse(UInt16 condReg, Label label) {
        EmitOp(OpCode.JUMP_IF_FALSE);
        EmitU16(condReg);
        int patchPos = pos;
        EmitI32(0);
        fixups.Add(new Fixup(patchPos, label));
    }


    public void EmitJumpIfTrue(UInt16 firstIndex, Label label) {
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
    public void EmitU16(UInt16 value) {
        byte[] conValue = BitConverter.GetBytes(value);
        emittedBytecode.AddRange(conValue);
    }

    public void EmitLoadConstant(UInt16 dstReg, UInt16 constIndex) {
        EmitOp(OpCode.LOAD_CONST);
        EmitU16(dstReg);
        EmitU16(constIndex);
    }

    public void EmitStoreLocal(UInt16 srcReg, UInt16 localIndex) {
        EmitOp(OpCode.STORE_LOCAL);
        EmitU16(srcReg);
        EmitU16(localIndex);
    }

    public void EmitLoadLocal(UInt16 dstReg, UInt16 localIndex) {
        EmitOp(OpCode.LOAD_LOCAL);
        EmitU16(dstReg);
        EmitU16(localIndex);
    }

    public void EmitReturn(UInt16 returnReg) {
        EmitOp(OpCode.RETURN);
        EmitU16(returnReg);
    }

    private void EmitRRR(OpCode op, UInt16 dstReg, UInt16 leftReg, UInt16 rightReg) {
        EmitOp(op);
        EmitU16(dstReg);
        EmitU16(leftReg);
        EmitU16(rightReg);
    }

    public void EmitAddInt(UInt16 dstReg, UInt16 leftReg, UInt16 rightReg) {
        EmitRRR(OpCode.ADD_INT, dstReg, leftReg, rightReg);
    }

    public void EmitSubtractInt(UInt16 dstReg, UInt16 leftReg, UInt16 rightReg) {
        EmitRRR(OpCode.SUBTRACT_INT, dstReg, leftReg, rightReg);
    }

    public void EmitMultiplyInt(UInt16 dstReg, UInt16 leftReg, UInt16 rightReg) {
        EmitRRR(OpCode.MULTIPLY_INT, dstReg, leftReg, rightReg);
    }

    public void EmitDivideInt(UInt16 dstReg, UInt16 leftReg, UInt16 rightReg) {
        EmitRRR(OpCode.DIVIDE_INT, dstReg, leftReg, rightReg);
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
    public void EmitCmpLTInt(UInt16 dstReg, UInt16 leftReg, UInt16 rightReg) {
        EmitRRR(OpCode.CMP_LT_INT, dstReg, leftReg, rightReg);
    }

    public void EmitCmpLTEInt(UInt16 dstReg, UInt16 leftReg, UInt16 rightReg) {
        EmitRRR(OpCode.CMP_LTE_INT, dstReg, leftReg, rightReg);
    }

    public void EmitCmpMTInt(UInt16 dstReg, UInt16 leftReg, UInt16 rightReg) {
        EmitRRR(OpCode.CMP_MT_INT, dstReg, leftReg, rightReg);
    }

    public void EmitCmpMTEInt(UInt16 dstReg, UInt16 leftReg, UInt16 rightReg) {
        EmitRRR(OpCode.CMP_MTE_INT, dstReg, leftReg, rightReg);
    }
    public void EmitCmpEQ(UInt16 dstReg, UInt16 leftReg, UInt16 rightReg) {
        EmitRRR(OpCode.CMP_EQ, dstReg, leftReg, rightReg);
    }
    public void EmitCmpNEQ(UInt16 dstReg, UInt16 leftReg, UInt16 rightReg) {
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
}
