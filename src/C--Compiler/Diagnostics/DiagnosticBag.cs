using System.Linq;

namespace CMinus.Compiler.Diagnostics;

public class DiagnosticBag {
    readonly List<Diagnostic> diagnostics = new();
    public string? SourceText { get; set; }

    public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;
    public bool HasErrors => diagnostics.Any(diagnostic => diagnostic.severity == Severity.Error);

    public void Report(Diagnostic diagnostic) {
        diagnostics.Add(diagnostic);
    }
    public void Report(DiagnosticDescriptor descriptor, TextSpan textSpan, params object[] args) {
        diagnostics.Add(new Diagnostic(descriptor, textSpan, args));
    }

    public bool CheckForErrors() {
        return HasErrors;
    }

    public void PrintAllErrors() {
        foreach (Diagnostic diagnostic in diagnostics) {
            if (diagnostic.severity == Severity.Error) {
                Console.WriteLine(diagnostic.ToDisplayString(SourceText));
                PrintSourceSnippet(diagnostic);
            }
        }
    }

    void PrintSourceSnippet(Diagnostic diagnostic) {
        if (string.IsNullOrEmpty(SourceText)) {
            return;
        }

        string sourceText = SourceText;
        int sourceLength = sourceText.Length;
        int start = Math.Max(0, Math.Min(diagnostic.textSpan.Start, sourceLength));
        int end = Math.Max(start, Math.Min(start + Math.Max(1, diagnostic.textSpan.Length), sourceLength));

        int lineStart = start;
        while (lineStart > 0 && sourceText[lineStart - 1] != '\n') {
            lineStart--;
        }

        int lineEnd = start;
        while (lineEnd < sourceLength && sourceText[lineEnd] != '\n') {
            lineEnd++;
        }

        string lineText = sourceText.Substring(lineStart, lineEnd - lineStart).TrimEnd('\r');
        int lineNumber = Diagnostic.GetLineAndColumn(sourceText, start).line;
        int caretColumn = Math.Max(0, start - lineStart);
        int caretLength = Math.Max(1, Math.Min(end - start, Math.Max(1, lineText.Length - caretColumn)));

        Console.WriteLine("  " + lineNumber + " | " + lineText);
        Console.WriteLine("    | " + new string(' ', caretColumn) + new string('^', caretLength));
    }
}
