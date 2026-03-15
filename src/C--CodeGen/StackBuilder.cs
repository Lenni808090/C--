namespace CMinus.CodeGen;

using CMinus.Compiler.Lowering;
class StackBuilder {
    List<FuncByteoffsetStackMap> funcByteoffsetStackMaps;
    List<ByteoffsetStackMap> byteoffsetStacks;
    FunctionStackMap[] functionStacks;

    Dictionary<(int blockId, int instrIndex), StackMap> posToStack;

    public StackBuilder(FunctionStackMap[] functionStacks) {
        posToStack = new();
        byteoffsetStacks = new();
        funcByteoffsetStackMaps = new();
        this.functionStacks = functionStacks;
    }

    public void FillPosToStack(int functionInd) {
        var functionStackMap = functionStacks[functionInd];

        foreach (StackMap stack in functionStackMap.stackMaps) {
            posToStack[(stack.blockId, stack.instrIndex)] = stack;
        }
    }

    public void TryRecordByteoffsetStackMap(int blockId, int instrIndex, int offset) {
        if (posToStack.TryGetValue((blockId, instrIndex), out StackMap stackMap)) {
            var byteoffset = new ByteoffsetStackMap(offset, stackMap.liveRegs, stackMap.liveLocals);
            byteoffsetStacks.Add(byteoffset);
        }
    }

    public FuncByteoffsetStackMap BuildFunctionByteoffsetStackMap() {
        var funcStackMap = new FuncByteoffsetStackMap(byteoffsetStacks.ToArray());
        funcByteoffsetStackMaps.Add(funcStackMap);
        return funcStackMap;
    }

    public FuncByteoffsetStackMap[] GetByteoffsetStackMap() {
        return funcByteoffsetStackMaps.ToArray();
    }

    public void Reset() {
        byteoffsetStacks.Clear();
        posToStack.Clear();
    }
}

class FuncByteoffsetStackMap {
    public ByteoffsetStackMap[] byteoffsetStackMaps;

    public FuncByteoffsetStackMap(ByteoffsetStackMap[] byteoffsetStackMaps) {
        this.byteoffsetStackMaps = byteoffsetStackMaps;
    }
}

struct ByteoffsetStackMap {
    public int byteoffset;
    public ulong[] liveRegMaskWords;
    public ulong[] liveLocalMaskWords;

    public ByteoffsetStackMap(int byteoffset, bool[] liveRegs, bool[] liveLocals) {
        this.byteoffset = byteoffset;
        liveRegMaskWords = PackBitMask(liveRegs);
        liveLocalMaskWords = PackBitMask(liveLocals);
    }

    static ulong[] PackBitMask(bool[] bits) {
        int wordCount = (bits.Length + 63) / 64;
        ulong[] words = new ulong[wordCount];

        for (int i = 0; i < bits.Length; i++) {
            if (!bits[i]) {
                continue;
            }

            int wordIndex = i / 64;
            int bitIndex = i % 64;
            words[wordIndex] |= 1UL << bitIndex;
        }

        return words;
    }
}
