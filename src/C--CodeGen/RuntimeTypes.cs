namespace CMinus.CodeGen;

enum RuntimeTypeKind {
    Int,
    Bool,
    Char,
    Array,
    Object,
}

sealed class RuntimeTypeDesc {
    public uint TypeId;
    public RuntimeTypeKind Kind;
    public uint? ElementTypeId;
    public string? Name;

    public RuntimeTypeDesc(uint typeId, RuntimeTypeKind kind, uint? elementTypeId = null, string? name = null) {
        TypeId = typeId;
        Kind = kind;
        ElementTypeId = elementTypeId;
        Name = name;
    }

    public override string ToString() {
        return Kind switch {
            RuntimeTypeKind.Array => $"#{TypeId}: Array({ElementTypeId})",
            RuntimeTypeKind.Object => $"#{TypeId}: Object({Name})",
            _ => $"#{TypeId}: {Kind}",
        };
    }
}
