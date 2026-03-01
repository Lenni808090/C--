namespace CMinus.Compiler.Binding;

abstract class BoundStmt { };
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

    public BoundIfStmt(BoundExpr boundConditionExpr, BoundStmt thenStmt) {
        this.boundConditionExpr = boundConditionExpr;
        this.thenStmt = thenStmt;
    }
}

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


sealed class LocalSymbol {
    public string name;
    public SymbolType symbolType;
    public int index;

    public LocalSymbol(string name, SymbolType symbolType, int index) {
        this.name = name;
        this.symbolType = symbolType;
        this.index = index;
    }
}




enum SymbolType {
    Int,
    Bool,
}
