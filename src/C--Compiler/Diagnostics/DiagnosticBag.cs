using System.Linq;

namespace CMinus.Compiler.Diagnostics;

public class DiagnosticBag {
    readonly List<Diagnostic> diagnostics = new();
    public string? SourceText {
        get; set;
    }

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
            }
        }
    }
}
