using System.Security.Cryptography.X509Certificates;

namespace CMinus.Compiler.Binding;

sealed class BoundLabelStmt : BoundStmt {
    public LabelSymbol label;

    public BoundLabelStmt(LabelSymbol label) {
        this.label = label;
    }
}

sealed class BoundGotoStmt : BoundStmt {
    public LabelSymbol gotoLabel;

    public BoundGotoStmt(LabelSymbol gotoLabel) {
        this.gotoLabel = gotoLabel;
    }
}

sealed class BoundConditionalGotoStmt : BoundStmt {
    public BoundExpr condition;
    public LabelSymbol target;
    public bool jumpIfTrue;

    public BoundConditionalGotoStmt(BoundExpr condition, LabelSymbol target, bool jumpIfTrue) {
        this.condition = condition;
        this.target = target;
        this.jumpIfTrue = jumpIfTrue;
    }
}



class LabelSymbol {

};