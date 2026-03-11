namespace CMinus.Compiler.Lowering;

using ValType = CMinus.Runtime.ValueType;

sealed class ConstantInterner {
    readonly Dictionary<(ValType, long), int> cache = new();
    readonly List<CMinus.Runtime.Value> constants = new();

    public int Intern(ValType type, long rawValue) {
        var key = (type, rawValue);
        if (cache.TryGetValue(key, out int existing)) {
            return existing;
        }

        int id = constants.Count;
        cache[key] = id;
        constants.Add(new CMinus.Runtime.Value(type, rawValue));
        return id;
    }

    public CMinus.Runtime.Value[] Build() => constants.ToArray();
}
