using System;
using System.Collections.Generic;
using CMinus.Compiler;
using CMinus.Runtime;

namespace CMinus.CodeGen;

sealed class FunctionBuilder {
    public Emitter Emitter { get; } = new();
    public Value[] Constants => constants.ToArray();
    public InstructionDebugInfo[] DebugInfo => debugInfo.ToArray();

    private readonly Dictionary<Value, int> constantToIndex = new();
    private readonly List<Value> constants = new();
    private readonly List<InstructionDebugInfo> debugInfo = new();

    public int AddConstant(Value value) {
        if (constantToIndex.TryGetValue(value, out int existing)) {
            return existing;
        }

        int index = constants.Count;
        constantToIndex.Add(value, index);
        constants.Add(value);
        return index;
    }

    public void RecordLocation(SourceLocation location) {
        if (!location.IsValid) {
            return;
        }

        int bytecodeOffset = Emitter.pos;
        if (debugInfo.Count > 0 && debugInfo[^1].BytecodeOffset == bytecodeOffset) {
            return;
        }

        debugInfo.Add(new InstructionDebugInfo(bytecodeOffset, location));
    }

    public CompiledFunction Build(int localCount, int paramCount, int maxRegCount) {
        if (localCount < 0) {
            throw new ArgumentOutOfRangeException(nameof(localCount));
        }

        Emitter.PatchAll();

        return new CompiledFunction(
            Emitter.BytecodeToArray(),
            localCount,
            maxRegCount,
            paramCount,
            DebugInfo
        );
    }

    public CompiledFunction BuildAndReset(int localCount, int paramCount, int maxRegCount) {
        var func = Build(localCount, paramCount, maxRegCount);
        Emitter.Reset();
        return func;
    }
}
