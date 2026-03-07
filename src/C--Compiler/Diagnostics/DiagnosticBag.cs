using System.Linq;
using CMinus.Compiler;

namespace CMinus.Compiler.Diagnostics;

public class DiagnosticBag {
    readonly List<Diagnostic> diagnostics = new();

    public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;
    public bool HasErrors => diagnostics.Any(diagnostic => diagnostic.severity == Severity.Error);

    public void Report(Diagnostic diagnostic) {
        diagnostics.Add(diagnostic);
    }

    public void Report(DiagnosticDescriptor descriptor, params object[] args) {
        diagnostics.Add(new Diagnostic(descriptor, args));
    }

    internal void Report(SourceLocation location, DiagnosticDescriptor descriptor, params object[] args) {
        diagnostics.Add(new Diagnostic(location, descriptor, args));
    }

    internal void Report(Token token, DiagnosticDescriptor descriptor, params object[] args) {
        Report(token.Location, descriptor, args);
    }

    public bool CheckForErrors() {
        return HasErrors;
    }

    public void PrintAllErrors() {
        foreach (Diagnostic diagnostic in diagnostics) {
            if (diagnostic.severity == Severity.Error) {
                Console.WriteLine(diagnostic.ToDisplayString());
            }
        }
    }
}
