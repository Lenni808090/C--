namespace CMinus.Compiler.Binding;

using CMinus.Compiler;

abstract class BoundStmt {
    public SourceLocation location { get; }

    protected BoundStmt(SourceLocation location) {
        this.location = location;
    }
};
abstract class BoundExpr {
    public SymbolType type {
        get;
    }
    public SourceLocation location { get; }
    //nur die classe und andere inheriting ones are able to acess;
    protected BoundExpr(SymbolType type, SourceLocation location) {
        this.type = type;
        this.location = location;
    }
};

sealed class BoundCompiledUnit : BoundStmt {
    public BoundStmt[] boundStmts;
    public int localCount;

    public BoundCompiledUnit(BoundStmt[] boundStmts, int localCount) : base(boundStmts.Length > 0 ? boundStmts[0].location : SourceLocation.None) {
        this.boundStmts = boundStmts;
        this.localCount = localCount;
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

    public BoundVarAssignmentExpr(LocalSymbol localSymbol, BoundExpr assignmentExpr, SymbolType type, SourceLocation location) : base(type, location) {
        this.localSymbol = localSymbol;
        this.assignmentExpr = assignmentExpr;
    }
}

sealed class BoundLiteralExpr : BoundExpr {
    public long value;

    public BoundLiteralExpr(long value, SymbolType type, SourceLocation location) : base(type, location) {
        this.value = value;
    }
}

sealed class BoundNameExpr : BoundExpr {
    public LocalSymbol localSymbol;
    //works because first normal conbstructure then base constructur is called;
    public BoundNameExpr(LocalSymbol localSymbol, SourceLocation location) : base(localSymbol.symbolType, location) {
        this.localSymbol = localSymbol;
    }
}

sealed class BoundBinaryExpr : BoundExpr {
    public BoundExpr leftBoundExpr;
    public BoundExpr rightBoundExpr;
    public BoundBinaryOperator boundBinaryOperator;
    public BoundBinaryExpr(BoundExpr leftBoundExpr, BoundExpr rightBoundExpr, BoundBinaryOperator boundBinaryOperatorKind, SymbolType symbolType, SourceLocation location) : base(symbolType, location) {
        this.leftBoundExpr = leftBoundExpr;
        this.rightBoundExpr = rightBoundExpr;
        this.boundBinaryOperator = boundBinaryOperatorKind;
    }
}

sealed class BoundUnaryExpr : BoundExpr {
    public BoundExpr operatedExpr;
    public BoundUnaryOperator boundUnaryOperator;

    public BoundUnaryExpr(BoundExpr operatedExpr, BoundUnaryOperator boundUnaryOperator, SymbolType symbolType, SourceLocation location) : base(symbolType, location) {
        this.operatedExpr = operatedExpr;
        this.boundUnaryOperator = boundUnaryOperator;
    }
}

sealed class BoundErrorExpr : BoundExpr {
    public BoundErrorExpr(SourceLocation location) : base(SymbolType.DiagnosticsError, location) { }
}

sealed class BoundErrorStmt : BoundStmt {
    public BoundErrorStmt(SourceLocation location) : base(location) { }
}


sealed class LocalSymbol {
    public string name;
    public SymbolType symbolType;

    public BoundModifiers modifiers;
    public bool isCompilerGenerated;
    public int index;

    public LocalSymbol(string name, SymbolType symbolType, BoundModifiers modifiers, int index, bool isCompilerGenerated = false) {
        this.modifiers = modifiers;
        this.name = name;
        this.symbolType = symbolType;
        this.index = index;
        this.isCompilerGenerated = isCompilerGenerated;
    }

    public static LocalSymbol generateTempLocal(SymbolType symbolType, int index) {
        string name = "&temp" + index;

        return new LocalSymbol(name, symbolType, new BoundModifiers(), index, true);
    }
}



class BoundModifiers {
    public bool isMutable = false;

}
enum SymbolType {
    Int,
    Bool,

    DiagnosticsError,
}
