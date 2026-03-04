namespace CMinus.Compiler.Diagnostics;

public static class DiagnosticDescriptors {
    public static readonly DiagnosticDescriptor LexerUnexpectedSinglePipe =
        new("LEX001", "Lexer", "Unexpected single '|'. Did you mean '||'?", Severity.Error);
    public static readonly DiagnosticDescriptor LexerUnexpectedSingleAmpersand =
        new("LEX002", "Lexer", "Unexpected single '&'. Did you mean '&&'?", Severity.Error);
    public static readonly DiagnosticDescriptor LexerUnknownCharacter =
        new("LEX003", "Lexer", "Unknown character in input: '{0}'", Severity.Error);

    public static readonly DiagnosticDescriptor ParserUnexpectedToken =
        new("PAR001", "Parser", "{0} got {1}", Severity.Error);
    public static readonly DiagnosticDescriptor ParserMissingClosingBrace =
        new("PAR002", "Parser", "closing brace expected after body", Severity.Error);
    public static readonly DiagnosticDescriptor ParserExpectedPrimaryExpression =
        new("PAR003", "Parser", "Expected primary expression, got {0} '{1}'", Severity.Error);

    public static readonly DiagnosticDescriptor BinderConditionMustBeBool =
        new("BND001", "Binder", "condition must be of type bool", Severity.Error);
    public static readonly DiagnosticDescriptor BinderVarAlreadyDeclared =
        new("BND002", "Binder", "var already declared in this scope: {0}", Severity.Error);
    public static readonly DiagnosticDescriptor BinderDeclaredAndAssignedTypeMismatch =
        new("BND003", "Binder", "declared and assigned type are not the same", Severity.Error);
    public static readonly DiagnosticDescriptor BinderVariableNotDeclared =
        new("BND004", "Binder", "this var is not declared: {0}", Severity.Error);
    public static readonly DiagnosticDescriptor BinderNumberLiteralMissingValue =
        new("BND005", "Binder", "number literal needs to have a value", Severity.Error);
    public static readonly DiagnosticDescriptor BinderUnexpectedLiteralType =
        new("BND006", "Binder", "unexpected literal type: {0}", Severity.Error);
    public static readonly DiagnosticDescriptor BinderBinaryTypeMismatch =
        new("BND007", "Binder", "type mismatch in binary operation", Severity.Error);
    public static readonly DiagnosticDescriptor BinderUnknownTypeToken =
        new("BND008", "Binder", "unknown type {0}", Severity.Error);
    public static readonly DiagnosticDescriptor BinderUnknownTokenType =
        new("BND009", "Binder", "unkown type {0}", Severity.Error);

    public static readonly DiagnosticDescriptor ConrolFlowUnreachableCode = new("CFA001", "Control Flow Analysis", "unreachable code detected", Severity.Error);
}
