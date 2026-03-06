using System.Reflection.Emit;
using CMinus.Compiler;

namespace CMinus.Compiler.Syntax;


abstract class SyntaxNode {
    public abstract SyntaxKind syntaxKind {
        get;
    }
}
abstract class TypeSyntax : SyntaxNode { };
abstract class Stmt : SyntaxNode { };
abstract class Expr : SyntaxNode { };

//seperated for expandability later;
sealed class IdentifierTypeSyntax : TypeSyntax {
    public override SyntaxKind syntaxKind => SyntaxKind.IdentifierType;

    public Token identifier;

    public IdentifierTypeSyntax(Token identifier) {
        this.identifier = identifier;
    }
}


sealed class CompilationUnit : SyntaxNode {
    public Stmt[] stmts;


    public override SyntaxKind syntaxKind => SyntaxKind.CompilationUnit;
    public CompilationUnit(Stmt[] stmts) {
        this.stmts = stmts;
    }
}

sealed class ExpressionStmt : Stmt {
    public override SyntaxKind syntaxKind => SyntaxKind.ExpressionStmt;
    public Expr Expression;

    public ExpressionStmt(Expr expression) {
        Expression = expression;
    }
}
sealed class ReturnStmt : Stmt {
    public override SyntaxKind syntaxKind => SyntaxKind.ReturnStmt;
    public Expr returnExpr;

    public ReturnStmt(Expr returnExpr) {
        this.returnExpr = returnExpr;
    }

}

sealed class IfStmt : Stmt {
    public override SyntaxKind syntaxKind => SyntaxKind.IfStmt;
    public Expr condition;
    public Stmt thenStmt;
    public Stmt? elseStmt;

    public IfStmt(Expr condition, Stmt thenStmt, Stmt? elseStmt = null) {
        this.condition = condition;
        this.thenStmt = thenStmt;
        this.elseStmt = elseStmt;
    }
}


sealed class WhileStmt : Stmt {
    public override SyntaxKind syntaxKind => SyntaxKind.WhileStmt;
    public Expr condition;
    public Stmt body;

    public WhileStmt(Expr condition, Stmt body) {
        this.condition = condition;
        this.body = body;
    }
}


sealed class ForStmt : Stmt {
    public override SyntaxKind syntaxKind => SyntaxKind.ForStmt;

    public VarDeclarationStmt? declarationStmt;

    public Expr? initializeExpr;
    public Expr condition;
    public Expr iteration;
    public Stmt body;

    public ForStmt(VarDeclarationStmt? declarationStmt, Expr? initializeExpr, Expr condition, Expr iteration, Stmt body) {
        this.declarationStmt = declarationStmt;
        this.initializeExpr = initializeExpr;
        this.condition = condition;
        this.iteration = iteration;
        this.body = body;
    }
}

sealed class ContinueStmt : Stmt {
    public override SyntaxKind syntaxKind => SyntaxKind.ContinueStmt;
}
sealed class BreakStmt : Stmt {
    public override SyntaxKind syntaxKind => SyntaxKind.BreakStmt;
}
sealed class BlockStmt : Stmt {
    public override SyntaxKind syntaxKind => SyntaxKind.BlockStmt;
    public Stmt[] stmts;

    public BlockStmt(Stmt[] stmts) {
        this.stmts = stmts;
    }
}

sealed class VarDeclarationStmt : Stmt {
    public override SyntaxKind syntaxKind => SyntaxKind.VarDeclarationStmt;
    public TypeSyntax type;
    public Token name;

    public Expr declarementExpr;

    public VarDeclarationStmt(TypeSyntax type, Token name, Expr declarementExpr) {
        this.type = type;
        this.name = name;
        this.declarementExpr = declarementExpr;
    }
}

sealed class VarAssignmentExpr : Expr {
    public override SyntaxKind syntaxKind => SyntaxKind.VarAssignmentStmt;

    public Token variable;
    public Expr assignmentExpr;

    public VarAssignmentExpr(Token variable, Expr assignmentExpr) {
        this.variable = variable;
        this.assignmentExpr = assignmentExpr;
    }

}

sealed class LiteralExpr : Expr {
    public override SyntaxKind syntaxKind => SyntaxKind.LiteralExpr;
    public Token value;


    public LiteralExpr(Token value) {
        this.value = value;
    }
}

sealed class NameExpr : Expr {
    public override SyntaxKind syntaxKind => SyntaxKind.NameExpr;
    public Token name;

    public NameExpr(Token name) {
        this.name = name;
    }

}

sealed class BinaryExpr : Expr {
    public Expr leftExpr;
    public Expr rightExpr;

    public Token Operator;
    public override SyntaxKind syntaxKind => SyntaxKind.BinaryExpr;

    public BinaryExpr(Expr leftExpr, Token Operator, Expr rightExpr) {
        this.leftExpr = leftExpr;
        //grr operator keyword;
        this.Operator = Operator;
        this.rightExpr = rightExpr;
    }
}

sealed class UnaryExpr : Expr {
    public override SyntaxKind syntaxKind => SyntaxKind.UnaryExpr;

    public Token Operator;
    public Expr operatedExpr;

    public UnaryExpr(Token Operator, Expr operatedExpr) {
        this.Operator = Operator;
        this.operatedExpr = operatedExpr;
    }
}

enum SyntaxKind {
    ReturnStmt,
    VarDeclarationStmt,
    VarAssignmentStmt,
    IfStmt,
    WhileStmt,
    ForStmt,
    BlockStmt,
    ContinueStmt,
    BreakStmt,
    CompilationUnit,


    LiteralExpr,
    NameExpr,
    BinaryExpr,
    UnaryExpr,
    ExpressionStmt,


    IdentifierType,
}
