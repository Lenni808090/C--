using System;

namespace CMinus.CodeGen;

sealed class FunctionBuilder {
    public Emitter Emitter { get; } = new();

    public CompiledFunction Build(int localCount, int paramCount, int maxRegCount) {
        if (localCount < 0) {
            throw new ArgumentOutOfRangeException(nameof(localCount));
        }

        Emitter.PatchAll();

        return new CompiledFunction(
            Emitter.BytecodeToArray(),
            localCount,
            maxRegCount,
            paramCount
        );
    }

    public CompiledFunction BuildAndReset(int localCount, int paramCount, int maxRegCount) {
        var func = Build(localCount, paramCount, maxRegCount);
        Emitter.Reset();
        return func;
    }
}
