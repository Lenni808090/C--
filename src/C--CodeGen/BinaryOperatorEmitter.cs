using CMinus.Compiler.Binding;

namespace CMinus.CodeGen;

static class BinaryOperatorEmitter {
    static readonly Dictionary<BoundBinaryOperatorKind, Action<Emitter, byte, byte, byte>> BinaryEmitters = new() {
        { BoundBinaryOperatorKind.AddInt, (e, l, r, t) => e.EmitAddInt(l, r, t) },
        { BoundBinaryOperatorKind.SubtractInt, (e, l, r, t) => e.EmitSubtractInt(l, r, t) },
        { BoundBinaryOperatorKind.MultiplyInt, (e, l, r, t) => e.EmitMultiplyInt(l, r, t) },
        { BoundBinaryOperatorKind.DivideInt, (e, l, r, t) => e.EmitDivideInt(l, r, t) },

        { BoundBinaryOperatorKind.EqualsInt, (e, l, r, t) => e.EmitCmpEQInt(l, r, t) },
        { BoundBinaryOperatorKind.LessThanInt, (e, l, r, t) => e.EmitCmpLTInt(l, r, t) },
        { BoundBinaryOperatorKind.LessThanOrEqualInt, (e, l, r, t) => e.EmitCmpLTEInt(l, r, t) },
        { BoundBinaryOperatorKind.GreaterThanInt, (e, l, r, t) => e.EmitCmpMTInt(l, r, t) },
        { BoundBinaryOperatorKind.GreaterThanOrEqualInt, (e, l, r, t) => e.EmitCmpMTEInt(l, r, t) },
    };

    static public Action<Emitter, byte, byte, byte>? getEmitMethod(BoundBinaryOperatorKind boundBinaryOperatorKind) {
        if (BinaryEmitters.TryGetValue(boundBinaryOperatorKind, out var emit)) {
            return emit;
        }
        return null;
    }
}
