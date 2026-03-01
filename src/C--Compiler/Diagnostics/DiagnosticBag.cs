using System.Linq;

namespace CMinus.Compiler.Diagnostics;

public class DiagnosticBag {
    readonly List<Diagnostic> diagnostics = new();

    public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;
    public bool HasErrors => diagnostics.Any(diagnostic => diagnostic.severity == Severity.Error);

    public void Report(Diagnostic diagnostic) {
        diagnostics.Add(diagnostic);
    }

    public bool CheckForErrors() {
        return HasErrors;
    }

    public void PrintAllErrors() {
        foreach (Diagnostic diagnostic in diagnostics) {
            if (diagnostic.severity == Severity.Error) {
                Console.WriteLine(diagnostic.ToString());
            }
        }
    }

}
