namespace CMinus.Runtime;

enum RuntimeTypeKind {
    Int,
    Bool,
    Char,
    Array,
    Object,
}

sealed class RuntimeTypeDesc {
    public int TypeId {
        get;
    }

    public RuntimeTypeKind Kind {
        get;
    }

    public int? ElementTypeId {
        get;
    }

    public string? Name {
        get;
    }

    public RuntimeTypeDesc(int typeId, RuntimeTypeKind kind, int? elementTypeId = null, string? name = null) {
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
