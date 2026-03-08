namespace CMinus.Compiler.Diagnostics;

public static class DiagnosticDescriptors {
    public static readonly DiagnosticDescriptor LexerUnexpectedSinglePipe =
        new("LEX001", "Lexer", "Unexpected single '|'. Did you mean '||'?", Severity.Error);
    public static readonly DiagnosticDescriptor LexerUnexpectedSingleAmpersand =
        new("LEX002", "Lexer", "Unexpected single '&'. Did you mean '&&'?", Severity.Error);
    public static readonly DiagnosticDescriptor LexerUnknownCharacter =
        new("LEX003", "Lexer", "Unknown character in input: '{0}'", Severity.Error);
    public static readonly DiagnosticDescriptor LexerCharLiteratureTooLong =
        new("LEX003", "Lexer", "Char literature either only one char or Valid Escape Code'", Severity.Error);
    public static readonly DiagnosticDescriptor ParserUnexpectedToken =
        new("PAR001", "Parser", "{0} got {1}", Severity.Error);
    public static readonly DiagnosticDescriptor ParserMissingClosingBrace =
        new("PAR002", "Parser", "closing brace expected after body", Severity.Error);
    public static readonly DiagnosticDescriptor ParserExpectedPrimaryExpression =
        new("PAR003", "Parser", "Expected primary expression, got {0} '{1}'", Severity.Error);
    public static readonly DiagnosticDescriptor ParserDeclarationExpectedAfterModifiers =
        new("PAR004", "Parser", "Expected a declaration stmt after modifiers", Severity.Error);
    public static readonly DiagnosticDescriptor ParserForLoopNeedsAssignmentOrDeclaration =
        new("PAR005", "Parser", "A For Loop needs a Declarratrion or Assignment.Can not be empty", Severity.Error);



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
    public static readonly DiagnosticDescriptor BinderNotInLoopContinue =
        new("BND010", "Binder", "usage of continue outside of a loop", Severity.Error);
    public static readonly DiagnosticDescriptor BinderNotInLoopBreak =
        new("BND011", "Binder", "usage of break outside of a loop", Severity.Error);

    public static readonly DiagnosticDescriptor BinderDuplicateModifier =
        new("BND012", "Binder", "only one modifier of each kind per decl", Severity.Error);
    public static readonly DiagnosticDescriptor BinderUnkownModifier =
        new("BND013", "Binder", "Unkown modifier used infront of declaration", Severity.Error);

    public static readonly DiagnosticDescriptor BinderInmutableAssignment =
        new("BND014", "Binder", "Cant assign new value to inmutable var try addding mut ", Severity.Error);

    public static readonly DiagnosticDescriptor BinderTopLevelStmtMustBeFunction =
        new("BND015", "Binder", "top-level statements must be function declarations", Severity.Error);
    public static readonly DiagnosticDescriptor BinderFunctionAlreadyDeclared =
        new("BND016", "Binder", "function already declared: {0}", Severity.Error);
    public static readonly DiagnosticDescriptor BinderDuplicateParameterName =
        new("BND017", "Binder", "parameter names must be unique: {0}", Severity.Error);
    public static readonly DiagnosticDescriptor BinderProgramNeedsEntryPoint =
        new("BND018", "Binder", "program needs an entry point named Main", Severity.Error);
    public static readonly DiagnosticDescriptor BinderFunctionResolutionFailed =
        new("BND019", "Binder", "function could not be resolved after collection: {0}", Severity.Error);
    public static readonly DiagnosticDescriptor BinderReturnTypeMismatch =
        new("BND020", "Binder", "return type mismatch. expected {0}, got {1}", Severity.Error);
    public static readonly DiagnosticDescriptor BinderUnexpectedStatement =
        new("BND021", "Binder", "unsupported statement in binder: {0}", Severity.Error);
    public static readonly DiagnosticDescriptor BinderUnexpectedExpression =
        new("BND022", "Binder", "unsupported expression in binder: {0}", Severity.Error);
    public static readonly DiagnosticDescriptor BinderCallTargetMustBeFunctionName =
        new("BND023", "Binder", "call target must be a function name", Severity.Error);
    public static readonly DiagnosticDescriptor BinderFunctionNotDeclared =
        new("BND024", "Binder", "function is not declared: {0}", Severity.Error);
    public static readonly DiagnosticDescriptor BinderCallArgumentCountMismatch =
        new("BND025", "Binder", "function '{0}' expects {1} argument(s), got {2}", Severity.Error);
    public static readonly DiagnosticDescriptor BinderCallArgumentTypeMismatch =
        new("BND026", "Binder", "argument {1} of function '{0}' expects {2}, got {3}", Severity.Error);


    public static readonly DiagnosticDescriptor ConrolFlowUnreachableCode = new("CFA001", "Control Flow Analysis", "unreachable code detected", Severity.Error);
    public static readonly DiagnosticDescriptor ControlFLowAllPathsNeedReturn = new("CFA002", "Control Flow Analysis", "all paths need to return a value", Severity.Error);
}
