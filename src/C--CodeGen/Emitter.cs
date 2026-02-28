using System.ComponentModel;

class Emitter {
    List<byte> emittedBytecode;
    List<Fixup> fixups;
    Dictionary<string, int> labelPos;
    int pos => emittedBytecode.Count;

    public Emitter() {
        fixups = new();
        labelPos = new();
        emittedBytecode = new();
    }



    public void EmitJump(string labelName) {
        emittedBytecode.Add((byte)OpCode.JUMP);
        int patchPos = pos;
        EmitI32(0);
        fixups.Add(new Fixup(patchPos, labelName));
    }

    public void EmitJumpIfFalse(byte firstIndex, string labelName) {
        emittedBytecode.Add((byte)OpCode.JUMP_IF_FALSE);
        emittedBytecode.Add((byte)firstIndex);
        int patchPos = pos;
        EmitI32(0);
        fixups.Add(new Fixup(patchPos, labelName));
    }


    public void EmitJumpIfTrue(byte firstIndex, string labelName) {
        emittedBytecode.Add((byte)OpCode.JUMP_IF_TRUE);
        emittedBytecode.Add((byte)firstIndex);
        int patchPos = pos;
        EmitI32(0);
        fixups.Add(new Fixup(patchPos, labelName));
    }
    public void DefineLabel(string name) {
        labelPos.Add(name, pos);
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

    public void EmitAddInt(byte dstReg, byte leftReg, byte rightReg) {
        EmitOp(OpCode.ADD_INT);
        EmitU8(dstReg);
        EmitU8(leftReg);
        EmitU8(rightReg);
    }

    public void EmitOp(OpCode opCode) {
        emittedBytecode.Add((byte)opCode);
    }

    public void PatchAll() {
        foreach (Fixup fixup in fixups) {
            if (labelPos.TryGetValue(fixup.LableName, out int foundPos)) {
                // plus 4 because the jump starts after the 4 bytes for offset are read
                int offset = foundPos - (fixup.PatchPos + 4);
                var bytes = BitConverter.GetBytes(offset);
                for (int i = 0; i < 4; i++) {
                    emittedBytecode[i + fixup.PatchPos] = bytes[i];
                }
            }
        }
    }

    public byte[] ToArray() {
        return emittedBytecode.ToArray();
    }
}