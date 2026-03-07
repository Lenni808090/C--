namespace CMinus.Compiler.Diagnostics;

using CMinus.Compiler;

public sealed class DiagnosticDescriptor {
    public string Code { get; }
    public string Category { get; }
    public string MessageFormat { get; }
    public Severity Severity { get; }

    public DiagnosticDescriptor(string code, string category, string messageFormat, Severity severity) {
        Code = code;
        Category = category;
        MessageFormat = messageFormat;
        Severity = severity;
    }
}

public class Diagnostic {
    public string code;
    public string category;
    public string message;
    public Severity severity;
    public SourceLocation? location;

    public Diagnostic(string message, Severity severity) {
        this.code = "GEN000";
        this.category = "General";
        this.message = message;
        this.severity = severity;
        location = null;
    }

    public Diagnostic(DiagnosticDescriptor descriptor, params object[] args) {
        code = descriptor.Code;
        category = descriptor.Category;
        message = string.Format(descriptor.MessageFormat, args);
        severity = descriptor.Severity;
        location = null;
    }

    internal Diagnostic(SourceLocation location, DiagnosticDescriptor descriptor, params object[] args) : this(descriptor, args) {
        this.location = location;
    }

    public string ToDisplayString() {
        if (location is SourceLocation sourceLocation && sourceLocation.IsValid) {
            return severity + " " + code + " at " + sourceLocation + ": " + message;
        }

        return severity + " " + code + ": " + message;
    }

    public override string ToString() {
        return ToDisplayString();
    }
}

public enum Severity {
    Error,
    Warning
}
