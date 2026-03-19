using System;

namespace CMinus.CodeGen;

sealed class FunctionBuilder {
    public Emitter Emitter { get; } = new();

    public CompiledFunction Build(int localCount, int paramCount, int maxRegCount, FuncByteoffsetStackMap functionStackMap) {
        if (localCount < 0) {
            throw new ArgumentOutOfRangeException(nameof(localCount));
        }

        Emitter.PatchAll();

        return new CompiledFunction(
            Emitter.BytecodeToArray(),
            localCount,
            maxRegCount,
            paramCount,
            functionStackMap
        );
    }

    public CompiledFunction BuildAndReset(int localCount, int paramCount, int maxRegCount, FuncByteoffsetStackMap functionStackMap) {
        var func = Build(localCount, paramCount, maxRegCount, functionStackMap);
        Emitter.Reset();
        return func;
    }
}
