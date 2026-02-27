class FunctionBuilder {

    Emitter emitter;

    List<Value> constants;
    Dictionary<string, int> locals;
    int nextLocalIndex;


    public FunctionBuilder() {
        emitter = new();
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
        constants.Add(value);
        return constants.Count - 1;
    }
}