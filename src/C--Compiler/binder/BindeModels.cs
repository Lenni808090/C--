namespace CMinus.Compiler.Binding;

abstract class BoundStmt {
};
abstract class BoundExpr {
    public SymbolType type {
        get;
    }
    //nur die classe und andere inheriting ones are able to acess;
    protected BoundExpr(SymbolType type) {
        this.type = type;
    }
};

sealed class BoundCompiledUnit : BoundStmt {
    public BoundStmt[] boundStmts;
    public int localCount;

    public BoundCompiledUnit(BoundStmt[] boundStmts, int localCount) {
        this.boundStmts = boundStmts;
        this.localCount = localCount;
    }
}

sealed class BoundVarDeclarationStmt : BoundStmt {
    public LocalSymbol localSymbol;
    public BoundExpr initializer;

    public BoundVarDeclarationStmt(LocalSymbol localSymbol, BoundExpr initializer) {
        this.localSymbol = localSymbol;
        this.initializer = initializer;
    }
}


sealed class BoundIfStmt : BoundStmt {
    public BoundExpr boundConditionExpr;
    public BoundStmt thenStmt;

    public BoundStmt? elseStmt;
    public BoundIfStmt(BoundExpr boundConditionExpr, BoundStmt thenStmt, BoundStmt? elseStmt = null) {
        this.boundConditionExpr = boundConditionExpr;
        this.thenStmt = thenStmt;
        this.elseStmt = elseStmt;
    }
}

sealed class BoundWhileStmt : BoundStmt {
    public BoundExpr boundConditionExpr;
    public BoundStmt body;
    public BoundWhileStmt(BoundExpr boundConditionExpr, BoundStmt body) {
        this.boundConditionExpr = boundConditionExpr;
        this.body = body;
    }
}

sealed class BoundForStmt : BoundStmt {
    public BoundStmt initializer;
    public BoundExpr condition;

    public BoundExpr iteration;

    public BoundStmt body;

    public BoundForStmt(BoundStmt initializer, BoundExpr condition, BoundExpr iteration, BoundStmt body) {
        this.initializer = initializer;
        this.condition = condition;
        this.iteration = iteration;
        this.body = body;
    }
}

sealed class BoundBreakStmt : BoundStmt { };

sealed class BoundContinueStmt : BoundStmt { };


sealed class BoundBlockStmt : BoundStmt {
    public BoundStmt[] boundStmts;

    public BoundBlockStmt(BoundStmt[] boundStmts) {
        this.boundStmts = boundStmts;
    }
}

sealed class BoundReturnStmt : BoundStmt {
    public BoundExpr boundReturnedExpr;

    public BoundReturnStmt(BoundExpr boundReturnedExpr) {
        this.boundReturnedExpr = boundReturnedExpr;
    }
}
sealed class BoundExpressionStmt : BoundStmt {
    public BoundExpr boundExpr;
    public BoundExpressionStmt(BoundExpr boundExpr) {
        this.boundExpr = boundExpr;
    }
}







sealed class BoundVarAssignmentExpr : BoundExpr {
    public LocalSymbol localSymbol;
    public BoundExpr assignmentExpr;

    public BoundVarAssignmentExpr(LocalSymbol localSymbol, BoundExpr assignmentExpr, SymbolType type) : base(type) {
        this.localSymbol = localSymbol;
        this.assignmentExpr = assignmentExpr;
    }
}

sealed class BoundLiteralExpr : BoundExpr {
    public long value;

    public BoundLiteralExpr(long value, SymbolType type) : base(type) {
        this.value = value;
    }
}

sealed class BoundNameExpr : BoundExpr {
    public LocalSymbol localSymbol;
    //works because first normal conbstructure then base constructur is called;
    public BoundNameExpr(LocalSymbol localSymbol) : base(localSymbol.symbolType) {
        this.localSymbol = localSymbol;
    }
}

sealed class BoundBinaryExpr : BoundExpr {
    public BoundExpr leftBoundExpr;
    public BoundExpr rightBoundExpr;
    public BoundBinaryOperator boundBinaryOperator;
    public BoundBinaryExpr(BoundExpr leftBoundExpr, BoundExpr rightBoundExpr, BoundBinaryOperator boundBinaryOperatorKind, SymbolType symbolType) : base(symbolType) {
        this.leftBoundExpr = leftBoundExpr;
        this.rightBoundExpr = rightBoundExpr;
        this.boundBinaryOperator = boundBinaryOperatorKind;
    }
}

sealed class BoundUnaryExpr : BoundExpr {
    public BoundExpr operatedExpr;
    public BoundUnaryOperator boundUnaryOperator;

    public BoundUnaryExpr(BoundExpr operatedExpr, BoundUnaryOperator boundUnaryOperator, SymbolType symbolType) : base(symbolType) {
        this.operatedExpr = operatedExpr;
        this.boundUnaryOperator = boundUnaryOperator;
    }
}

sealed class BoundErrorExpr : BoundExpr {
    public BoundErrorExpr() : base(SymbolType.DiagnosticsError) { }
}

sealed class BoundErrorStmt : BoundStmt {

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
