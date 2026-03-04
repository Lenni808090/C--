using CMinus.Compiler;
using CMinus.Compiler.Diagnostics;
using CMinus.Compiler.Lowering;

class ControlFlowAnalyser {
    readonly IrCompiledUnit irCompiledUnit;
    readonly DiagnosticBag diagnostics;

    public ControlFlowAnalyser(IrCompiledUnit irCompiledUnit, CompilerContext compilerContext) {
        this.irCompiledUnit = irCompiledUnit;
        diagnostics = compilerContext.diagnostics;
    }

    public IrCompiledUnit Analyse() {
        BasicBlock[] blocks = irCompiledUnit.basicBlocks;
        for (int i = 0; i < blocks.Length; i++) {
            var block = blocks[i];
            if (!block.isUnreachable) {
                continue;
            }

            diagnostics.Report(DiagnosticDescriptors.ConrolFlowUnreachableCode, block.sourceSpan);
        }

        return irCompiledUnit;
    }
}
