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
}

