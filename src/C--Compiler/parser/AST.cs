using System.Reflection.Emit;
using CMinus.Compiler;

namespace CMinus.Compiler.Syntax;


abstract class SyntaxNode {
    public abstract SyntaxKind syntaxKind {
        get;
    }

    public abstract SourceLocation location {
        get;
    }
}
abstract class TypeSyntax : SyntaxNode { };
abstract class Stmt : SyntaxNode { };
abstract class Expr : SyntaxNode { };

//seperated for expandability later;
sealed class IdentifierTypeSyntax : TypeSyntax {
    public override SyntaxKind syntaxKind => SyntaxKind.IdentifierType;
    public override SourceLocation location => identifier.Location;

    public Token identifier;

    public IdentifierTypeSyntax(Token identifier) {
        this.identifier = identifier;
    }
}


sealed class CompilationUnit : SyntaxNode {
    public Stmt[] stmts;
    public override SourceLocation location => stmts.Length > 0 ? stmts[0].location : SourceLocation.None;

    public override SyntaxKind syntaxKind => SyntaxKind.CompilationUnit;
    public CompilationUnit(Stmt[] stmts) {
        this.stmts = stmts;
    }
}

sealed class ExpressionStmt : Stmt {
    public override SyntaxKind syntaxKind => SyntaxKind.ExpressionStmt;
    public override SourceLocation location => Expression.location;
    public Expr Expression;

    public ExpressionStmt(Expr expression) {
        Expression = expression;
    }
}
sealed class ReturnStmt : Stmt {
    public override SyntaxKind syntaxKind => SyntaxKind.ReturnStmt;
    public override SourceLocation location => keyword.Location;
    public Token keyword;
    public Expr returnExpr;

    public ReturnStmt(Token keyword, Expr returnExpr) {
        this.keyword = keyword;
        this.returnExpr = returnExpr;
    }

}

sealed class IfStmt : Stmt {
    public override SyntaxKind syntaxKind => SyntaxKind.IfStmt;
    public override SourceLocation location => condition.location;
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
    public override SourceLocation location => condition.location;
    public Expr condition;
    public Stmt body;

    public WhileStmt(Expr condition, Stmt body) {
        this.condition = condition;
        this.body = body;
    }
}


sealed class ForStmt : Stmt {
    public override SyntaxKind syntaxKind => SyntaxKind.ForStmt;
    public override SourceLocation location => declarationStmt?.location ?? initializeExpr?.location ?? condition.location;

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
    public override SourceLocation location => keyword.Location;
    public Token keyword;

    public ContinueStmt(Token keyword) {
        this.keyword = keyword;
    }
}
sealed class BreakStmt : Stmt {
    public override SyntaxKind syntaxKind => SyntaxKind.BreakStmt;
    public override SourceLocation location => keyword.Location;
    public Token keyword;

    public BreakStmt(Token keyword) {
        this.keyword = keyword;
    }
}
sealed class BlockStmt : Stmt {
    public override SyntaxKind syntaxKind => SyntaxKind.BlockStmt;
    public override SourceLocation location => stmts.Length > 0 ? stmts[0].location : openBrace.Location;
    public Token openBrace;
    public Stmt[] stmts;

    public BlockStmt(Token openBrace, Stmt[] stmts) {
        this.openBrace = openBrace;
        this.stmts = stmts;
    }
}

sealed class VarDeclarationStmt : Stmt {
    public override SyntaxKind syntaxKind => SyntaxKind.VarDeclarationStmt;
    public override SourceLocation location => modifiers.Length > 0 ? modifiers[0].Location : type.location;

    public Token[] modifiers;

    public TypeSyntax type;
    public Token name;

    public Expr declarementExpr;

    public VarDeclarationStmt(Token[] modifiers, TypeSyntax type, Token name, Expr declarementExpr) {
        this.modifiers = modifiers;
        this.type = type;
        this.name = name;
        this.declarementExpr = declarementExpr;
    }
}

sealed class VarAssignmentExpr : Expr {
    public override SyntaxKind syntaxKind => SyntaxKind.VarAssignmentStmt;
    public override SourceLocation location => variable.Location;

    public Token variable;

    public Token assignmentOperator;
    public Expr assignmentExpr;

    public VarAssignmentExpr(Token variable, Token assignmentOperator, Expr assignmentExpr) {
        this.assignmentOperator = assignmentOperator;
        this.variable = variable;
        this.assignmentExpr = assignmentExpr;
    }

}

sealed class LiteralExpr : Expr {
    public override SyntaxKind syntaxKind => SyntaxKind.LiteralExpr;
    public override SourceLocation location => value.Location;
    public Token value;


    public LiteralExpr(Token value) {
        this.value = value;
    }
}

sealed class NameExpr : Expr {
    public override SyntaxKind syntaxKind => SyntaxKind.NameExpr;
    public override SourceLocation location => name.Location;
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
    public override SourceLocation location => Operator.Location;

    public BinaryExpr(Expr leftExpr, Token Operator, Expr rightExpr) {
        this.leftExpr = leftExpr;
        //grr operator keyword;
        this.Operator = Operator;
        this.rightExpr = rightExpr;
    }
}

sealed class UnaryExpr : Expr {
    public override SyntaxKind syntaxKind => SyntaxKind.UnaryExpr;
    public override SourceLocation location => Operator.Location;

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
