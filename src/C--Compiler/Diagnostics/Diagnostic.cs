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
    public TextSpan textSpan;
    public Severity severity;

    public Diagnostic(string message, Severity severity) {
        this.code = "GEN000";
        this.category = "General";
        this.message = message;
        this.severity = severity;
        this.textSpan = TextSpan.None;
    }

    public Diagnostic(string message, Severity severity, TextSpan textSpan) {
        this.code = "GEN000";
        this.category = "General";
        this.message = message;
        this.severity = severity;
        this.textSpan = textSpan;
    }

    public Diagnostic(DiagnosticDescriptor descriptor, TextSpan textSpan, params object[] args) {
        code = descriptor.Code;
        category = descriptor.Category;
        message = string.Format(descriptor.MessageFormat, args);
        severity = descriptor.Severity;
        this.textSpan = textSpan;
    }

    public string ToDisplayString(string? sourceText) {
        int end = textSpan.Start + textSpan.Length;
        if (string.IsNullOrEmpty(sourceText)) {
            return severity + " " + code + ": " + message + " @[" + textSpan.Start + ".." + end + ")";
        }

        var (line, column) = GetLineAndColumn(sourceText, textSpan.Start);
        return severity + " " + code + ": " + message + " at " + line + ":" + column + " @[" + textSpan.Start + ".." + end + ")";
    }

    public override string ToString() {
        return ToDisplayString(null);
    }

    public static (int line, int column) GetLineAndColumn(string sourceText, int position) {
        int clamped = Math.Max(0, Math.Min(position, sourceText.Length));

        int line = 1;
        int lineStart = 0;
        for (int i = 0; i < clamped; i++) {
            if (sourceText[i] == '\n') {
                line++;
                lineStart = i + 1;
            }
        }

        int column = clamped - lineStart + 1;
        return (line, column);
    }
}

public struct TextSpan {
    public int Start;
    public int Length;

    public TextSpan(int start, int length) {
        Start = start;
        Length = length;
    }

    public static TextSpan None => new TextSpan(0, 0);
}

public enum Severity {
    Error,
    Warning
}
