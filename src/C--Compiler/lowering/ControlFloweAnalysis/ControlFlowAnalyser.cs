using CMinus.Compiler;
using CMinus.Compiler.Diagnostics;
using CMinus.Compiler.Lowering;

class ControlFlowAnalyser {
    readonly IrCompiledUnit irCompiledUnit;
    readonly DiagnosticBag diagnostics;

    Dictionary<int, BasicBlock> idToBlock;
    public ControlFlowAnalyser(IrCompiledUnit irCompiledUnit, CompilerContext compilerContext) {
        this.irCompiledUnit = irCompiledUnit;
        diagnostics = compilerContext.diagnostics;
        idToBlock = new();
        FillIdToBlock();
    }

    public IrCompiledUnit Analyse() {
        BasicBlock[] blocks = irCompiledUnit.basicBlocks;
        var visited = new HashSet<int>();
        // popinter way faster then array shifting
        int head = 0;
        var worklist = new List<BasicBlock> { blocks[0] };

        while (head < worklist.Count) {

            var currBlock = worklist[head++];

            // hashet fast check if contains + adding as visited;
            if (!visited.Add(currBlock.blockId)) {
                continue;
            }

            switch (currBlock.terminator) {
                case IrReturn:
                    break;
                case IrGoto g:
                    worklist.Add(idToBlock[g.basicBlockId]);
                    break;
                case IrBranch b:
                    worklist.Add(idToBlock[b.thenBlockId]);
                    worklist.Add(idToBlock[b.elseBlockId]);
                    break;
                default:
                    throw new Exception("unkown terminator");
            }
        }

        List<BasicBlock> reached = new();
        foreach (BasicBlock block in blocks) {
            if (visited.Contains(block.blockId)) {
                reached.Add(block);
            }
            else {
                diagnostics.Report(DiagnosticDescriptors.ConrolFlowUnreachableCode, block.sourceSpan);
            }
        }

        return new IrCompiledUnit(reached.ToArray(), irCompiledUnit.localCount, irCompiledUnit.maxVReg);
    }

    void FillIdToBlock() {
        BasicBlock[] blocks = irCompiledUnit.basicBlocks;
        foreach (BasicBlock basic in blocks) {
            idToBlock[basic.blockId] = basic;
        }
    }
}
