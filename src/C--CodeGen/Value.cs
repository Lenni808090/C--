namespace CMinus.CodeGen;

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
        return Type switch {
            ValueType.Int => $"Int({RawData})",
            ValueType.Bool => $"Bool({(RawData == 1 ? "true" : "false")})",
            ValueType.Char => $"Char({(char)RawData})",
            ValueType.HeapRef => $"HeapRef({RawData})",
            ValueType.Null => "Null",
            _ => throw new InvalidOperationException("Unknown ValueType: " + Type),
        };
    }
}
