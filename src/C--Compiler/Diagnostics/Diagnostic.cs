namespace CMinus.Compiler.Diagnostics;

public class Diagnostic {
    public string message;
    public TextSpan textSpan;
    public Severity severity;

    public Diagnostic(string message, Severity severity) {
        this.message = message;
        this.severity = severity;
        this.textSpan = TextSpan.None;
    }

    public Diagnostic(string message, Severity severity, TextSpan textSpan) {
        this.message = message;
        this.severity = severity;
        this.textSpan = textSpan;
    }

    public override string ToString() {
        return severity + " " + message + " @[" + textSpan.Start + ".." + (textSpan.Start + textSpan.Length) + ")";
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
