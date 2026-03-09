namespace CMinus.Runtime;

enum ValueType : byte {
    Int,
    Bool,
    Char,

    HeapRef,

    Null,
}


struct Value {
    public ValueType Type;
    public long RawData;

    public Value(ValueType type, long rawData) {
        Type = type;
        RawData = rawData;
    }

    public int AsInt() {
        if (Type != ValueType.Int && Type != ValueType.Char) {
            throw new Exception("expected int");
        }
        return (int)RawData;
    }

    public bool AsBool() {
        if (Type != ValueType.Bool) {
            throw new Exception("expected bool");
        }
        return RawData != 0;
    }

    public int AsHeapRef() {
        if (Type != ValueType.HeapRef) {
            throw new Exception("expected heap ref");
        }
        return (int)RawData;
    }



    public override string ToString() {
        switch (Type) {
            case ValueType.Int: {
                    return $"Int({RawData})";
                }
            case ValueType.Bool: {
                    return $"Bool({(RawData == 1 ? "true" : "false")})";
                }
            case ValueType.Char: {
                    return $"Char({(char)RawData})";
                }
            case ValueType.HeapRef: {
                    return $"HeapRef({RawData})";
                }
            case ValueType.Null: {
                    return "Null";
                }
            default: {
                    throw new InvalidOperationException("Unknown ValueType: " + Type);
                }
        }
    }
}


struct HeapRef {
    public int id;
    public HeapRef(int id) {
        this.id = id;
    }
}
