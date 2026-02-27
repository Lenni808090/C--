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

    public void Jump(string labelName) {
        emittedBytecode.Add((byte)OpCode.JUMP);
        int patchPos = pos;
        EmitI32(0);
        fixups.Add(new Fixup(patchPos, labelName));
    }

    public void JumpIfFalse(byte firstIndex, string labelName) {
        emittedBytecode.Add((byte)OpCode.JUMP_IF_FALSE);
        emittedBytecode.Add((byte)firstIndex);
        int patchPos = pos;
        EmitI32(0);
        fixups.Add(new Fixup(patchPos, labelName));
    }


    public void JumpIfTrue(byte firstIndex, string labelName) {
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