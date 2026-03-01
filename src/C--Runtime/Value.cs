namespace CMinus.Runtime;

enum ValueType {
    Int,
    Bool,
}


struct Value {
    public ValueType ValueType;
    public long RawData;

    public Value(ValueType valueType, long rawData) {
        ValueType = valueType;
        RawData = rawData;
    }

    public bool AsBool() {
        return RawData == 1;
    }

    public override string ToString() {
        switch (ValueType) {
            case ValueType.Int: {
                    return $"Int({RawData})";
                }
            case ValueType.Bool: {
                    return $"Bool({(RawData == 1 ? "true" : "false")})";
                }
            default: {
                    throw new InvalidOperationException("Unknown ValueType: " + ValueType);
                }
        }
    }
}
