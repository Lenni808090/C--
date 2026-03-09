namespace CMinus.Compiler.Binding;

using CMinus.Compiler;
using CMinus.Compiler.Syntax;


abstract class BoundStmt {
    public SourceLocation location {
        get;
    }

    protected BoundStmt(SourceLocation location) {
        this.location = location;
    }
};
abstract class BoundExpr {
    public TypeSymbol type {
        get;
    }
    public SourceLocation location {
        get;
    }
    //nur die classe und andere inheriting ones are able to acess;
    protected BoundExpr(TypeSymbol type, SourceLocation location) {
        this.type = type;
        this.location = location;
    }
};

sealed class BoundCompiledUnit {
    public BoundFunctionDeclaration mainFunction;
    public BoundFunctionDeclaration[] functions;

    public BoundCompiledUnit(BoundFunctionDeclaration mainFunction, BoundFunctionDeclaration[] functions) {
        this.mainFunction = mainFunction;
        this.functions = functions;
    }
}


sealed class BoundVarDeclarationStmt : BoundStmt {
    public LocalSymbol localSymbol;
    public BoundExpr initializer;

    public BoundVarDeclarationStmt(LocalSymbol localSymbol, BoundExpr initializer, SourceLocation location) : base(location) {
        this.localSymbol = localSymbol;
        this.initializer = initializer;
    }
}

sealed class BoundFunctionDeclaration : BoundStmt {
    public FunctionSymbol functionSymbol;
    public BoundBlockStmt functionBody;

    public BoundFunctionDeclaration(FunctionSymbol functionSymbol, BoundBlockStmt functionBody, SourceLocation location) : base(location) {
        this.functionSymbol = functionSymbol;
        this.functionBody = functionBody;
    }
}


sealed class BoundIfStmt : BoundStmt {
    public BoundExpr boundConditionExpr;
    public BoundStmt thenStmt;

    public BoundStmt? elseStmt;
    public BoundIfStmt(BoundExpr boundConditionExpr, BoundStmt thenStmt, SourceLocation location, BoundStmt? elseStmt = null) : base(location) {
        this.boundConditionExpr = boundConditionExpr;
        this.thenStmt = thenStmt;
        this.elseStmt = elseStmt;
    }
}

sealed class BoundWhileStmt : BoundStmt {
    public BoundExpr boundConditionExpr;
    public BoundStmt body;
    public BoundWhileStmt(BoundExpr boundConditionExpr, BoundStmt body, SourceLocation location) : base(location) {
        this.boundConditionExpr = boundConditionExpr;
        this.body = body;
    }
}

sealed class BoundForStmt : BoundStmt {
    public BoundStmt initializer;
    public BoundExpr condition;

    public BoundExpr iteration;

    public BoundStmt body;

    public BoundForStmt(BoundStmt initializer, BoundExpr condition, BoundExpr iteration, BoundStmt body, SourceLocation location) : base(location) {
        this.initializer = initializer;
        this.condition = condition;
        this.iteration = iteration;
        this.body = body;
    }
}

sealed class BoundBreakStmt : BoundStmt {
    public BoundBreakStmt(SourceLocation location) : base(location) { }
};

sealed class BoundContinueStmt : BoundStmt {
    public BoundContinueStmt(SourceLocation location) : base(location) { }
};


sealed class BoundBlockStmt : BoundStmt {
    public BoundStmt[] boundStmts;

    public BoundBlockStmt(BoundStmt[] boundStmts, SourceLocation location) : base(location) {
        this.boundStmts = boundStmts;
    }
}

sealed class BoundReturnStmt : BoundStmt {
    public BoundExpr boundReturnedExpr;

    public BoundReturnStmt(BoundExpr boundReturnedExpr, SourceLocation location) : base(location) {
        this.boundReturnedExpr = boundReturnedExpr;
    }
}
sealed class BoundExpressionStmt : BoundStmt {
    public BoundExpr boundExpr;
    public BoundExpressionStmt(BoundExpr boundExpr, SourceLocation location) : base(location) {
        this.boundExpr = boundExpr;
    }
}







sealed class BoundVarAssignmentExpr : BoundExpr {
    public LocalSymbol localSymbol;
    public BoundExpr assignmentExpr;

    public BoundVarAssignmentExpr(LocalSymbol localSymbol, BoundExpr assignmentExpr, TypeSymbol type, SourceLocation location) : base(type, location) {
        this.localSymbol = localSymbol;
        this.assignmentExpr = assignmentExpr;
    }
}

sealed class BoundIndexAssignmentExpr : BoundExpr {
    public BoundExpr index;

    public LocalSymbol localSymbol;

    public BoundExpr assignmentExpr;

    public BoundIndexAssignmentExpr(BoundExpr index, LocalSymbol localSymbol, BoundExpr assignmentExpr, TypeSymbol type, SourceLocation location) : base(type, location) {
        this.index = index;
        this.localSymbol = localSymbol;
        this.assignmentExpr = assignmentExpr;
    }

}

sealed class BoundIndexExpr : BoundExpr {
    public BoundExpr index;

    public BoundExpr target;

    public BoundIndexExpr(BoundExpr index, BoundExpr target, TypeSymbol type, SourceLocation location) : base(type, location) {
        this.index = index;
        this.target = target;
    }
}

sealed class BoundArrayCreationExpr : BoundExpr {
    public BoundExpr length;

    public BoundArrayCreationExpr(BoundExpr length, TypeSymbol type, SourceLocation location) : base(type, location) {
        this.length = length;
    }
}

sealed class BoundCallExpr : BoundExpr {
    public BoundExpr[] args;

    public int argCount;
    public FunctionSymbol callee;

    public BoundCallExpr(BoundExpr[] args, FunctionSymbol calle, TypeSymbol type, SourceLocation location) : base(type, location) {
        this.callee = calle;
        this.args = args;
        argCount = args.Length;
    }
}

sealed class BoundLiteralExpr : BoundExpr {
    public long value;

    public BoundLiteralExpr(long value, TypeSymbol type, SourceLocation location) : base(type, location) {
        this.value = value;
    }
}

sealed class BoundNameExpr : BoundExpr {
    public LocalSymbol localSymbol;
    //works because first normal conbstructure then base constructur is called;
    public BoundNameExpr(LocalSymbol localSymbol, SourceLocation location) : base(localSymbol.typeSymbol, location) {
        this.localSymbol = localSymbol;
    }
}

sealed class BoundBinaryExpr : BoundExpr {
    public BoundExpr leftBoundExpr;
    public BoundExpr rightBoundExpr;
    public BoundBinaryOperator boundBinaryOperator;
    public BoundBinaryExpr(BoundExpr leftBoundExpr, BoundExpr rightBoundExpr, BoundBinaryOperator boundBinaryOperatorKind, TypeSymbol typeSymbol, SourceLocation location) : base(typeSymbol, location) {
        this.leftBoundExpr = leftBoundExpr;
        this.rightBoundExpr = rightBoundExpr;
        this.boundBinaryOperator = boundBinaryOperatorKind;
    }
}

sealed class BoundUnaryExpr : BoundExpr {
    public BoundExpr operatedExpr;
    public BoundUnaryOperator boundUnaryOperator;

    public BoundUnaryExpr(BoundExpr operatedExpr, BoundUnaryOperator boundUnaryOperator, TypeSymbol typeSymbol, SourceLocation location) : base(typeSymbol, location) {
        this.operatedExpr = operatedExpr;
        this.boundUnaryOperator = boundUnaryOperator;
    }
}

sealed class BoundErrorExpr : BoundExpr {
    public BoundErrorExpr(SourceLocation location) : base(BuiltInTypes.Error, location) { }
}

sealed class BoundErrorStmt : BoundStmt {
    public BoundErrorStmt(SourceLocation location) : base(location) { }
}


sealed class LocalSymbol {
    public string name;
    public TypeSymbol typeSymbol;

    public BoundModifiers modifiers;
    public bool isCompilerGenerated;
    public int index;

    public LocalSymbol(string name, TypeSymbol typeSymbol, BoundModifiers modifiers, int index, bool isCompilerGenerated = false) {
        this.modifiers = modifiers;
        this.name = name;
        this.typeSymbol = typeSymbol;
        this.index = index;
        this.isCompilerGenerated = isCompilerGenerated;
    }

    public static LocalSymbol generateTempLocal(TypeSymbol typeSymbol, int index) {
        string name = "&temp" + index;

        return new LocalSymbol(name, typeSymbol, new BoundModifiers(), index, true);
    }
}

sealed class FunctionSymbol {
    public string name;
    public TypeSymbol returnType;
    public int localCount;
    public int argCount;
    public TypeSymbol[] argTypes;

    public FunctionSymbol(string name, TypeSymbol returnType, TypeSymbol[] argTypes) {
        this.name = name;
        this.returnType = returnType;
        argCount = argTypes.Length;
        this.argTypes = argTypes;
    }
}



class BoundModifiers {
    public bool isMutable = false;

}


abstract class TypeSymbol {
    public string name;

    protected TypeSymbol(string name) {
        this.name = name;
    }

    public override string ToString() {
        return name;
    }
}

class PrimitiveSymbolType : TypeSymbol {
    public PrimitiveSymbolType(string name) : base(name) { }
}

class ArraySymbolType : TypeSymbol {
    public TypeSymbol elementType;

    public ArraySymbolType(TypeSymbol elementType) : base(elementType.name + "[]") {
        this.elementType = elementType;
    }

}

class ErrorSymbolType : TypeSymbol {
    public ErrorSymbolType() : base("Error") {
    }

}

static class TypeSymbolExtensions {
    public static bool IsSameType(this TypeSymbol type1, TypeSymbol type2) {
        if (ReferenceEquals(type1, type2)) {
            return true;
        }

        if (type1.GetType() != type2.GetType()) {
            return false;
        }

        switch (type1) {
            case PrimitiveSymbolType primitive1: {
                    var primitive2 = (PrimitiveSymbolType)type2;
                    return primitive1.name == primitive2.name;
                }
            case ArraySymbolType array1: {
                    var array2 = (ArraySymbolType)type2;
                    return IsSameType(array1.elementType, array2.elementType);
                }
            default: {
                    throw new Exception("Unkown type in type comparission");
                }
        }
    }
}

static class BuiltInTypes {
    public static readonly PrimitiveSymbolType Int = new("int");
    public static readonly PrimitiveSymbolType Char = new("char");
    public static readonly PrimitiveSymbolType Bool = new("bool");
    public static readonly PrimitiveSymbolType Error = new("<error>");
}
