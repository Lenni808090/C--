using System;
using System.Collections.Generic;

sealed class FunctionBuilder {
    public Emitter Emitter { get; } = new();

    private readonly Dictionary<Value, int> constantToIndex = new();
    private readonly List<Value> constants = new();

    public int AddConstant(Value value) {
        if (constantToIndex.TryGetValue(value, out int existing)) {
            return existing;
        }

        int index = constants.Count;
        constantToIndex.Add(value, index);
        constants.Add(value);
        return index;
    }

    public CompiledFunction Build(int localCount) {
        if (localCount < 0) {
            throw new ArgumentOutOfRangeException(nameof(localCount));
        }

        Emitter.PatchAll();

        return new CompiledFunction(
            Emitter.BytecodeToArray(),
            constants.ToArray(),
            localCount
        );
    }
}