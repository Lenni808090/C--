class FunctionBuilder {

    public Emitter Emitter { get; }

    Dictionary<Value, int> constantsIndex;
    List<Value> constants;
    Dictionary<string, int> locals;
    int nextLocalIndex;


    public FunctionBuilder() {
        Emitter = new();
        constantsIndex = new();
        constants = new();
        locals = new();
    }


    public int DeclareLocal(string name) {
        if (locals.ContainsKey(name)) {
            throw new Exception($"Local already declared: '{name}'");
        }

        int index = nextLocalIndex++;
        locals.Add(name, index);
        return index;
    }

    public int GetLocalIndex(string name) {
        if (!locals.TryGetValue(name, out int index)) {
            throw new Exception($"Unknown local: '{name}'");
        }

        return index;
    }

    public int AddConstant(Value value) {
        if (constantsIndex.TryGetValue(value, out int existing)) {
            return existing;
        }
        int index = constants.Count;
        constantsIndex.Add(value, index);
        constants.Add(value);
        return index;
    }

    public CompiledFunction Build() {
        Emitter.PatchAll();
        return new CompiledFunction(Emitter.BytecodeToArray(), constants.ToArray(), nextLocalIndex);
    }
}