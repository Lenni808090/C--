using System.ComponentModel;
using CMinus.Runtime;

namespace CMinus.CodeGen;

class Emitter {
    List<byte> emittedBytecode;
    List<Fixup> fixups;
    Dictionary<Label, int> labelPos;
    int pos => emittedBytecode.Count;

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

    public void EmitJumpIfFalse(byte condReg, Label label) {
        emittedBytecode.Add((byte)OpCode.JUMP_IF_FALSE);
        emittedBytecode.Add(condReg);
        int patchPos = pos;
        EmitI32(0);
        fixups.Add(new Fixup(patchPos, label));
    }


    public void EmitJumpIfTrue(byte firstIndex, Label label) {
        EmitOp(OpCode.JUMP_IF_FALSE);
        EmitU8(firstIndex);
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
    public void EmitU8(byte value) {
        emittedBytecode.Add((byte)value);
    }

    public void EmitLoadConstant(byte dstReg, byte constIndex) {
        EmitOp(OpCode.LOAD_CONST);
        EmitU8(dstReg);
        EmitU8(constIndex);
    }

    public void EmitStoreLocal(byte srcReg, byte localIndex) {
        EmitOp(OpCode.STORE_LOCAL);
        EmitU8(srcReg);
        EmitU8(localIndex);
    }

    public void EmitLoadLocal(byte dstReg, byte localIndex) {
        EmitOp(OpCode.LOAD_LOCAL);
        EmitU8(dstReg);
        EmitU8(localIndex);
    }

    public void EmitReturn(byte returnReg) {
        EmitOp(OpCode.RETURN);
        EmitU8(returnReg);
    }

    private void EmitRRR(OpCode op, byte dstReg, byte leftReg, byte rightReg) {
        EmitOp(op);
        EmitU8(dstReg);
        EmitU8(leftReg);
        EmitU8(rightReg);
    }

    public void EmitAddInt(byte dstReg, byte leftReg, byte rightReg) {
        EmitRRR(OpCode.ADD_INT, dstReg, leftReg, rightReg);
    }

    public void EmitSubtractInt(byte dstReg, byte leftReg, byte rightReg) {
        EmitRRR(OpCode.SUBTRACT_INT, dstReg, leftReg, rightReg);
    }

    public void EmitMultiplyInt(byte dstReg, byte leftReg, byte rightReg) {
        EmitRRR(OpCode.MULTIPLY_INT, dstReg, leftReg, rightReg);
    }

    public void EmitDivideInt(byte dstReg, byte leftReg, byte rightReg) {
        EmitRRR(OpCode.DIVIDE_INT, dstReg, leftReg, rightReg);
    }

    public void EmitCmpLTInt(byte dstReg, byte leftReg, byte rightReg) {
        EmitRRR(OpCode.CMP_LT_INT, dstReg, leftReg, rightReg);
    }

    public void EmitCmpLTEInt(byte dstReg, byte leftReg, byte rightReg) {
        EmitRRR(OpCode.CMP_LTE_INT, dstReg, leftReg, rightReg);
    }

    public void EmitCmpMTInt(byte dstReg, byte leftReg, byte rightReg) {
        EmitRRR(OpCode.CMP_MT_INT, dstReg, leftReg, rightReg);
    }

    public void EmitCmpMTEInt(byte dstReg, byte leftReg, byte rightReg) {
        EmitRRR(OpCode.CMP_MTE_INT, dstReg, leftReg, rightReg);
    }
    public void EmitCmpEQInt(byte dstReg, byte leftReg, byte rightReg) {
        EmitRRR(OpCode.CMP_EQ_INT, dstReg, leftReg, rightReg);
    }
    public void EmitCmpNEQInt(byte dstReg, byte leftReg, byte rightReg) {
        EmitRRR(OpCode.CMP_NEQ_INT, dstReg, leftReg, rightReg);
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
