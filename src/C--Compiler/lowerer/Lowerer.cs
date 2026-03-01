using CMinus.Compiler.Binding;

sealed class Lowerer {
    private readonly BoundCompiledUnit boundCompiledUnit;

    public Lowerer(BoundCompiledUnit boundCompiledUnit) {
        this.boundCompiledUnit = boundCompiledUnit;
    }

    public BoundCompiledUnit LowerCompiledUnit() {
        var lowered = new List<BoundStmt>();

        foreach (BoundStmt stmt in boundCompiledUnit.boundStmts) {
            LowerStmt(stmt, lowered);
        }

        return new BoundCompiledUnit(lowered.ToArray(), boundCompiledUnit.localCount);
    }

    private void LowerStmt(BoundStmt stmt, List<BoundStmt> output) {
        switch (stmt) {
            case BoundReturnStmt r: {
                    LowerReturnStmt(r, output);
                    break;
                }
            case BoundVarDeclarationStmt v: {
                    LowerVarDeclarationStmt(v, output);
                    break;
                }
            case BoundExpressionStmt e: {
                    LowerExpressionStmt(e, output);
                    break;
                }
            case BoundBlockStmt b: {
                    LowerBlockStmt(b, output);
                    break;
                }
            case BoundIfStmt i: {
                    LowerIfStmt(i, output);
                    break;
                }
            default: {
                    throw new Exception("unknown stmt in lowerer " + stmt);
                }
        }
    }

    private void LowerReturnStmt(BoundReturnStmt r, List<BoundStmt> output) {
        output.Add(new BoundReturnStmt(LowerExpr(r.boundReturnedExpr)));
    }

    private void LowerVarDeclarationStmt(BoundVarDeclarationStmt v, List<BoundStmt> output) {
        output.Add(new BoundVarDeclarationStmt(v.localSymbol, LowerExpr(v.initializer)));
    }

    private void LowerExpressionStmt(BoundExpressionStmt e, List<BoundStmt> output) {
        output.Add(new BoundExpressionStmt(LowerExpr(e.boundExpr)));
    }

    private void LowerBlockStmt(BoundBlockStmt b, List<BoundStmt> output) {
        var blockOut = new List<BoundStmt>();

        foreach (var s in b.boundStmts) {
            LowerStmt(s, blockOut);
        }

        output.Add(new BoundBlockStmt(blockOut.ToArray()));
    }

    private void LowerIfStmt(BoundIfStmt i, List<BoundStmt> output) {
        BoundExpr loweredCond = LowerExpr(i.boundConditionExpr);

        LabelSymbol endLabel = NewLabel();

        output.Add(new BoundConditionalGotoStmt(loweredCond, endLabel, jumpIfTrue: false));
        LowerStmt(i.thenStmt, output);
        output.Add(new BoundLabelStmt(endLabel));
    }

    private BoundExpr LowerExpr(BoundExpr expr) {
        return expr switch {
            BoundNameExpr n => n,
            BoundLiteralExpr l => l,
            BoundBinaryExpr b => LowerBinaryExpr(b),
            _ => throw new Exception("unknown expr in lowerer " + expr),
        };
    }

    private BoundExpr LowerBinaryExpr(BoundBinaryExpr b) {
        return new BoundBinaryExpr(
            LowerExpr(b.leftBoundExpr),
            LowerExpr(b.rightBoundExpr),
            b.boundBinaryOperator,
            b.type
        );
    }

    private LabelSymbol NewLabel() {
        return new LabelSymbol();
    }
}