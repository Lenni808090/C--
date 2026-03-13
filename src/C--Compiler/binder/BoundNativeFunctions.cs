using CMinus.Compiler.Binding;

class BoundNativeFunctions {
    static readonly FunctionSymbol[] NativeFunctions = {
        new("print_int",   BuiltInTypes.Void, new[] {BuiltInTypes.Int}, true),
        new("print_bool",   BuiltInTypes.Void, new[] {BuiltInTypes.Bool}, true),
        new("print_char",BuiltInTypes.Void, new[] {BuiltInTypes.Char}, true),
        new("print_newline",BuiltInTypes.Void, Array.Empty<TypeSymbol>(), true),
    };

    public static void FillFunctionDic(Dictionary<string, FunctionSymbol> functionsByName) {
        foreach (FunctionSymbol functionSymbol in NativeFunctions) {
            functionsByName[functionSymbol.name] = functionSymbol;
        }
    }
}