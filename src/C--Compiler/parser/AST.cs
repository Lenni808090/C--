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
sealed class ParameterSyntax : SyntaxNode {
    public Token name;
    public TypeSyntax type;
    public Token[] modifiers;
    public ParameterSyntax(Token name, TypeSyntax type, Token[] modifiers) {
        this.name = name;
        this.type = type;
        this.modifiers = modifiers;
    }

    public override SyntaxKind syntaxKind => SyntaxKind.ParameterSyntax;

    public override SourceLocation location => name.Location;
}
//seperated for expandability later;
sealed class IdentifierTypeSyntax : TypeSyntax {
    public override SyntaxKind syntaxKind => SyntaxKind.IdentifierType;
    public override SourceLocation location => identifier.Location;

    public Token identifier;

    public IdentifierTypeSyntax(Token identifier) {
        this.identifier = identifier;
    }
}

sealed class ArrayTypeSyntax : TypeSyntax {
    public override SyntaxKind syntaxKind => SyntaxKind.ArrayType;

    public override SourceLocation location => elementType.location;

    public TypeSyntax elementType;

    public ArrayTypeSyntax(TypeSyntax elementType) {
        this.elementType = elementType;
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
sealed class FunctionDeclarationStmt : Stmt {
    public override SyntaxKind syntaxKind => SyntaxKind.FunctionDeclaratioStmt;

    public override SourceLocation location => functionName.Location;

    public Token functionName;

    public ParameterSyntax[] @params;

    public TypeSyntax returnType;
    public BlockStmt functionBody;

    public FunctionDeclarationStmt(Token functionName, ParameterSyntax[] @params, TypeSyntax returnType, BlockStmt functionBody) {
        this.functionName = functionName;
        this.@params = @params;
        this.returnType = returnType;
        this.functionBody = functionBody;
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


sealed class ArrayCreationExpr : Expr {
    public override SyntaxKind syntaxKind => SyntaxKind.ArrayCreationExpr;

    public override SourceLocation location => typeSyntax.location;

    public TypeSyntax typeSyntax;

    public Expr length;

    public ArrayCreationExpr(TypeSyntax typeSyntax, Expr length) {
        this.typeSyntax = typeSyntax;
        this.length = length;
    }
}

sealed class CallExpr : Expr {
    public Expr calle;
    public Expr[] args;

    public override SyntaxKind syntaxKind => SyntaxKind.CallExpr;

    public override SourceLocation location => calle.location;
    public CallExpr(Expr calle, Expr[] args) {
        this.calle = calle;
        this.args = args;
    }
}

sealed class IndexExpr : Expr {
    public Expr target;
    public Expr index;

    public override SyntaxKind syntaxKind => SyntaxKind.IndexExpr;

    public override SourceLocation location => target.location;

    public IndexExpr(Expr target, Expr index) {
        this.target = target;
        this.index = index;
    }
}
sealed class AssignmentExpr : Expr {
    public override SyntaxKind syntaxKind => SyntaxKind.AssignmentExpr;
    public override SourceLocation location => target.location;

    public Expr target;

    public Token assignmentOperator;
    public Expr value;

    public AssignmentExpr(Expr target, Token assignmentOperator, Expr value) {
        this.assignmentOperator = assignmentOperator;
        this.target = target;
        this.value = value;
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
    ParameterSyntax,
    ReturnStmt,
    FunctionDeclaratioStmt,
    VarDeclarationStmt,
    IfStmt,
    WhileStmt,
    ForStmt,
    BlockStmt,
    ContinueStmt,
    BreakStmt,
    CompilationUnit,


    LiteralExpr,
    IndexExpr,
    NameExpr,
    BinaryExpr,
    ArrayCreationExpr,
    UnaryExpr,
    AssignmentExpr,
    ExpressionStmt,
    CallExpr,

    IdentifierType,
    ArrayType,
}
