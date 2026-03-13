namespace CMinus.Compiler.Lowering;

using CMinus.CodeGen;
using ValType = CMinus.CodeGen.ValueType;

sealed class ConstantInterner {
    readonly Dictionary<(ValType, long), int> cache = new();
    readonly List<Value> constants = new();

    public int Intern(ValType type, long rawValue) {
        var key = (type, rawValue);
        if (cache.TryGetValue(key, out int existing)) {
            return existing;
        }

        int id = constants.Count;
        cache[key] = id;
        constants.Add(new Value(type, rawValue));
        return id;
    }

    public Value[] Build() => constants.ToArray();
}
