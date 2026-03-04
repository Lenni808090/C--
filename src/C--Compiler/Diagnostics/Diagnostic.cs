namespace CMinus.Compiler.Diagnostics;

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

    public Diagnostic(string message, Severity severity) {
        this.code = "GEN000";
        this.category = "General";
        this.message = message;
        this.severity = severity;
    }

    public Diagnostic(DiagnosticDescriptor descriptor, params object[] args) {
        code = descriptor.Code;
        category = descriptor.Category;
        message = string.Format(descriptor.MessageFormat, args);
        severity = descriptor.Severity;
    }

    public string ToDisplayString() {
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
