
abstract class SyntaxNode {
    public abstract SyntaxKind syntaxKind { get; }
}
abstract class TypeSyntax : SyntaxNode { };
abstract class Stmt : SyntaxNode { };
abstract class Expr : SyntaxNode { };

//seperated for expandability later;
sealed class IdentifierTypeSyntax : TypeSyntax {
    public override SyntaxKind syntaxKind => SyntaxKind.IdentifierType;

    public Token Identifier;

    public IdentifierTypeSyntax(Token identifier) {
        Identifier = identifier;
    }
}
sealed class ReturnStmt : Stmt {
    public override SyntaxKind syntaxKind => SyntaxKind.ReturnStmt;
    public Expr returnExpr;

    public ReturnStmt(Expr returnExpr) {
        this.returnExpr = returnExpr;
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

    public BinaryExpr(Expr leftExpr, Expr rightExpr, Token Operator) {
        this.leftExpr = leftExpr;
        //grr operator keyword;
        this.Operator = Operator;
        this.rightExpr = rightExpr;
    }
}


enum SyntaxKind {
    ReturnStmt,
    VarDeclarationStmt,

    LiteralExpr,
    NameExpr,
    BinaryExpr,


    IdentifierType,
}