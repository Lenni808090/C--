using CMinus.Compiler.Binding;
using CMinus.Runtime;

namespace CMinus.Compiler.Lowering;

sealed class RuntimeTypeInterner {
    readonly Dictionary<string, int> keyToId = new();
    readonly List<RuntimeTypeDesc> types = new();

    public int Intern(TypeSymbol typeSymbol) {
        if (typeSymbol.IsSameType(BuiltInTypes.Error)) {
            throw new Exception("cannot intern error type");
        }

        string key = GetKey(typeSymbol);
        if (keyToId.TryGetValue(key, out int existingId)) {
            return existingId;
        }

        RuntimeTypeDesc desc;
        switch (typeSymbol) {
            case PrimitiveSymbolType primitive: {
                    int nextTypeId = types.Count;
                    desc = new RuntimeTypeDesc(nextTypeId, MapPrimitiveKind(primitive));
                    break;
                }
            case ArraySymbolType array: {
                    int elementTypeId = Intern(array.elementType);
                    int nextTypeId = types.Count;
                    desc = new RuntimeTypeDesc(nextTypeId, RuntimeTypeKind.Array, elementTypeId);
                    break;
                }
            case NamedTypeSymbol named: {
                    int nextTypeId = types.Count;
                    desc = new RuntimeTypeDesc(nextTypeId, RuntimeTypeKind.Object, name: named.name);
                    break;
                }
            default: {
                    throw new Exception("unsupported type symbol in runtime type interner: " + typeSymbol);
                }
        }

        int typeId = desc.TypeId;
        keyToId[key] = typeId;
        types.Add(desc);
        return typeId;
    }

    public RuntimeTypeDesc[] BuildTypeTable() {
        return types.ToArray();
    }

    string GetKey(TypeSymbol typeSymbol) {
        return typeSymbol switch {
            PrimitiveSymbolType primitive => "primitive:" + primitive.name,
            ArraySymbolType array => "array:" + GetKey(array.elementType),
            NamedTypeSymbol named => "object:" + named.name,
            _ => throw new Exception("unsupported type symbol in runtime type key: " + typeSymbol),
        };
    }

    RuntimeTypeKind MapPrimitiveKind(PrimitiveSymbolType primitive) {
        if (primitive.IsSameType(BuiltInTypes.Int)) {
            return RuntimeTypeKind.Int;
        }

        if (primitive.IsSameType(BuiltInTypes.Bool)) {
            return RuntimeTypeKind.Bool;
        }

        if (primitive.IsSameType(BuiltInTypes.Char)) {
            return RuntimeTypeKind.Char;
        }

        throw new Exception("unsupported primitive type in runtime type interner: " + primitive);
    }
}
