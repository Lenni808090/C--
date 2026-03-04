using CMinus.Compiler;

namespace CMinus.Compiler.Binding;

sealed class BoundBinaryOperator {
    public TokenType tokenType;
    public BoundBinaryOperatorKind operatorKind;
    public SymbolType leftType;
    public SymbolType rightType;

    public SymbolType resultType;

    public BoundBinaryOperator(
        TokenType tokenType,
        BoundBinaryOperatorKind operatorKind,
        SymbolType leftType,
        SymbolType rightType,
        SymbolType resultType) {
        this.tokenType = tokenType;
        this.operatorKind = operatorKind;
        this.leftType = leftType;
        this.rightType = rightType;
        this.resultType = resultType;
    }

    private static readonly BoundBinaryOperator[] operators = new[]
    {
        new BoundBinaryOperator(TokenType.Plus, BoundBinaryOperatorKind.AddInt, SymbolType.Int, SymbolType.Int, SymbolType.Int),
        new BoundBinaryOperator(TokenType.Minus, BoundBinaryOperatorKind.SubtractInt, SymbolType.Int, SymbolType.Int, SymbolType.Int),
        new BoundBinaryOperator(TokenType.Multiply, BoundBinaryOperatorKind.MultiplyInt, SymbolType.Int, SymbolType.Int, SymbolType.Int),
        new BoundBinaryOperator(TokenType.Divide, BoundBinaryOperatorKind.DivideInt, SymbolType.Int, SymbolType.Int, SymbolType.Int),

        new BoundBinaryOperator(TokenType.LessThen, BoundBinaryOperatorKind.LessThanInt, SymbolType.Int, SymbolType.Int, SymbolType.Bool),
        new BoundBinaryOperator(TokenType.LessThenEquals, BoundBinaryOperatorKind.LessThanOrEqualInt, SymbolType.Int, SymbolType.Int, SymbolType.Bool),
        new BoundBinaryOperator(TokenType.MoreThen, BoundBinaryOperatorKind.GreaterThanInt, SymbolType.Int, SymbolType.Int, SymbolType.Bool),
        new BoundBinaryOperator(TokenType.MoreThenEquals, BoundBinaryOperatorKind.GreaterThanOrEqualInt, SymbolType.Int, SymbolType.Int, SymbolType.Bool),

        new BoundBinaryOperator(TokenType.And, BoundBinaryOperatorKind.LogicalAnd, SymbolType.Bool, SymbolType.Bool, SymbolType.Bool),
        new BoundBinaryOperator(TokenType.Or, BoundBinaryOperatorKind.LogicalOr, SymbolType.Bool, SymbolType.Bool, SymbolType.Bool),
    };

    public static BoundBinaryOperator? GetBinaryOperator(TokenType tokenType, SymbolType leftType, SymbolType rightType) {
        var equalityOperatorKind = GetEqualityOperatorKind(tokenType);
        if (equalityOperatorKind is not null && leftType == rightType && IsEqualityComparable(leftType)) {
            return new BoundBinaryOperator(
                tokenType,
                equalityOperatorKind.Value,
                leftType,
                rightType,
                SymbolType.Bool
            );
        }

        foreach (var op in operators) {
            if (tokenType == op.tokenType && leftType == op.leftType && rightType == op.rightType) {
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

    static bool IsEqualityComparable(SymbolType type) {
        return type switch {
            SymbolType.Int => true,
            SymbolType.Bool => true,
            _ => false,
        };
    }
}

enum BoundBinaryOperatorKind {
    AddInt,
    SubtractInt,
    MultiplyInt,
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
