using System.Runtime.CompilerServices;
using CMinus.Compiler;

namespace CMinus.Compiler.Binding;

abstract class BinaryOperator {
    public abstract TokenType tokenType {
        get;
    }

}

sealed class BoundBinaryOperator : BinaryOperator {
    public override TokenType tokenType {
        get;
    }
    public BoundBinaryOperatorKind operatorKind;
    public TypeSymbol leftType;
    public TypeSymbol rightType;

    public TypeSymbol resultType;

    public BoundBinaryOperator(
        TokenType tokenType,
        BoundBinaryOperatorKind operatorKind,
        TypeSymbol leftType,
        TypeSymbol rightType,
        TypeSymbol resultType) {
        this.tokenType = tokenType;
        this.operatorKind = operatorKind;
        this.leftType = leftType;
        this.rightType = rightType;
        this.resultType = resultType;
    }

    private static readonly BoundBinaryOperator[] operators = new[]
    {
        new BoundBinaryOperator(TokenType.Plus, BoundBinaryOperatorKind.AddInt, BuiltInTypes.Int, BuiltInTypes.Int, BuiltInTypes.Int),
        new BoundBinaryOperator(TokenType.Minus, BoundBinaryOperatorKind.SubtractInt, BuiltInTypes.Int, BuiltInTypes.Int, BuiltInTypes.Int),
        new BoundBinaryOperator(TokenType.Multiply, BoundBinaryOperatorKind.MultiplyInt, BuiltInTypes.Int, BuiltInTypes.Int, BuiltInTypes.Int),
        new BoundBinaryOperator(TokenType.Divide, BoundBinaryOperatorKind.DivideInt, BuiltInTypes.Int, BuiltInTypes.Int, BuiltInTypes.Int),
        new BoundBinaryOperator(TokenType.Modulus, BoundBinaryOperatorKind.ModulusInt, BuiltInTypes.Int, BuiltInTypes.Int, BuiltInTypes.Int),

        new BoundBinaryOperator(TokenType.LessThen, BoundBinaryOperatorKind.LessThanInt, BuiltInTypes.Int, BuiltInTypes.Int, BuiltInTypes.Bool),
        new BoundBinaryOperator(TokenType.LessThenEquals, BoundBinaryOperatorKind.LessThanOrEqualInt, BuiltInTypes.Int, BuiltInTypes.Int, BuiltInTypes.Bool),
        new BoundBinaryOperator(TokenType.MoreThen, BoundBinaryOperatorKind.GreaterThanInt, BuiltInTypes.Int, BuiltInTypes.Int, BuiltInTypes.Bool),
        new BoundBinaryOperator(TokenType.MoreThenEquals, BoundBinaryOperatorKind.GreaterThanOrEqualInt, BuiltInTypes.Int, BuiltInTypes.Int, BuiltInTypes.Bool),
        new BoundBinaryOperator(TokenType.LessThen, BoundBinaryOperatorKind.LessThanInt, BuiltInTypes.Char, BuiltInTypes.Char, BuiltInTypes.Bool),
        new BoundBinaryOperator(TokenType.LessThenEquals, BoundBinaryOperatorKind.LessThanOrEqualInt, BuiltInTypes.Char, BuiltInTypes.Char, BuiltInTypes.Bool),
        new BoundBinaryOperator(TokenType.MoreThen, BoundBinaryOperatorKind.GreaterThanInt, BuiltInTypes.Char, BuiltInTypes.Char, BuiltInTypes.Bool),
        new BoundBinaryOperator(TokenType.MoreThenEquals, BoundBinaryOperatorKind.GreaterThanOrEqualInt, BuiltInTypes.Char, BuiltInTypes.Char, BuiltInTypes.Bool),

        new BoundBinaryOperator(TokenType.And, BoundBinaryOperatorKind.LogicalAnd, BuiltInTypes.Bool, BuiltInTypes.Bool, BuiltInTypes.Bool),
        new BoundBinaryOperator(TokenType.Or, BoundBinaryOperatorKind.LogicalOr, BuiltInTypes.Bool, BuiltInTypes.Bool, BuiltInTypes.Bool),
    };

    public static BoundBinaryOperator? GetBinaryOperator(TokenType tokenType, TypeSymbol leftType, TypeSymbol rightType) {
        var equalityOperatorKind = GetEqualityOperatorKind(tokenType);
        if (equalityOperatorKind is not null && leftType.IsSameType(rightType) && IsEqualityComparable(leftType)) {
            return new BoundBinaryOperator(
                tokenType,
                equalityOperatorKind.Value,
                leftType,
                rightType,
                BuiltInTypes.Bool
            );
        }

        foreach (var op in operators) {
            if (tokenType == op.tokenType && leftType.IsSameType(op.leftType) && rightType.IsSameType(op.rightType)) {
                return op;
            }
        }
        return null;
    }

    static BoundBinaryOperatorKind? GetEqualityOperatorKind(TokenType tokenType) {
        return tokenType switch {
            TokenType.EqualsEquals => BoundBinaryOperatorKind.Equals,
            TokenType.NotEquals => BoundBinaryOperatorKind.NotEquals,
            _ => null,
        };
    }

    static bool IsEqualityComparable(TypeSymbol type) {
        return type.IsSameType(BuiltInTypes.Int)
            || type.IsSameType(BuiltInTypes.Bool)
            || type.IsSameType(BuiltInTypes.Char);
    }
}

enum BoundBinaryOperatorKind {
    AddInt,
    SubtractInt,
    MultiplyInt,
    ModulusInt,
    DivideInt,

    Equals,
    NotEquals,

    LessThanInt,
    LessThanOrEqualInt,
    GreaterThanInt,
    GreaterThanOrEqualInt,

    LogicalAnd,
    LogicalOr,
}


sealed class BoundUnaryOperator : BinaryOperator {
    public override TokenType tokenType {
        get;
    }

    public BoundUnaryOperatorKind unaryOperatorKind;

    public TypeSymbol operandType;
    public TypeSymbol resultType;

    public BoundUnaryOperator(TokenType tokenType, TypeSymbol operandType, TypeSymbol resultType, BoundUnaryOperatorKind unaryOperatorKind) {
        this.tokenType = tokenType;
        this.operandType = operandType;
        this.unaryOperatorKind = unaryOperatorKind;
        this.resultType = resultType;
    }

    public static readonly BoundUnaryOperator[] boundUnaryOperators = new[] {
        new BoundUnaryOperator(TokenType.Bang, BuiltInTypes.Bool, BuiltInTypes.Bool, BoundUnaryOperatorKind.LogicalNot),
        new BoundUnaryOperator(TokenType.Minus, BuiltInTypes.Int, BuiltInTypes.Int, BoundUnaryOperatorKind.NegateInt),
};

    public static BoundUnaryOperator? GetUnaryOperator(TokenType tokenType, TypeSymbol operandType) {
        foreach (var op in boundUnaryOperators) {
            if (op.tokenType == tokenType && op.operandType.IsSameType(operandType)) {
                return op;
            }
        }

        return null;
    }
}


enum BoundUnaryOperatorKind {
    LogicalNot,
    NegateInt,
}
