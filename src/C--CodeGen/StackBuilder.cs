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

    public void TryRecordByteoffsetStackMap(int blockId, int instrIndex, uint offset) {
        if (posToStack.TryGetValue((blockId, instrIndex), out StackMap stackMap)) {
            var byteoffset = new ByteoffsetStackMap(offset, stackMap.liveRegs, stackMap.liveLocals);
            byteoffsetStacks.Add(byteoffset);
        }
    }

    public FuncByteoffsetStackMap BuildFunctionByteoffsetStackMap(ushort regWordCount, ushort localWordCount) {
        var funcStackMap = new FuncByteoffsetStackMap(byteoffsetStacks.ToArray(), regWordCount, localWordCount);
        funcByteoffsetStackMaps.Add(funcStackMap);
        return funcStackMap;
    }
    public void Reset() {
        byteoffsetStacks.Clear();
        posToStack.Clear();
    }
}

class FuncByteoffsetStackMap {
    public ByteoffsetStackMap[] byteoffsetStackMaps;
    public ushort regWordCount;
    public ushort localWordCount;

    public FuncByteoffsetStackMap(ByteoffsetStackMap[] byteoffsetStackMaps, ushort regWordCount, ushort localWordCount) {
        this.byteoffsetStackMaps = byteoffsetStackMaps;
        this.regWordCount = regWordCount;
        this.localWordCount = localWordCount;
    }
}


struct ByteoffsetStackMap {
    public uint byteoffset;
    public ulong[] liveRegMaskWords;
    public ulong[] liveLocalMaskWords;

    public ByteoffsetStackMap(uint byteoffset, bool[] liveRegs, bool[] liveLocals) {
        this.byteoffset = byteoffset;
        liveRegMaskWords = PackBitMask(liveRegs);
        liveLocalMaskWords = PackBitMask(liveLocals);
    }

    static ulong[] PackBitMask(bool[] bits) {
        //+63 because 3 / 64 gives 0 because of int rounding u need + 63 to get 1 
        // and with 63 + 63 u get 127 / 64 which is stil one.
        int wordCount = (bits.Length + 63) / 64;
        ulong[] words = new ulong[wordCount];

        for (int i = 0; i < bits.Length; i++) {
            if (!bits[i]) {
                continue;
            }

            int wordIndex = i / 64;
            //wraps arround 64
            int bitIndex = i % 64;
            //combines the two bits;
            words[wordIndex] |= 1UL << bitIndex;
        }

        return words;
    }
}
