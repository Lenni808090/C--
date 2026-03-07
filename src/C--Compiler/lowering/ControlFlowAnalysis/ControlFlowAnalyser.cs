using CMinus.Compiler;
using CMinus.Compiler.Diagnostics;

namespace CMinus.Compiler.Lowering;

class ControlFlowAnalyser {
    readonly IrCompiledUnit irCompiledUnit;
    readonly DiagnosticBag diagnostics;

    readonly Dictionary<int, BasicBlock> idToBlock;

    public ControlFlowAnalyser(IrCompiledUnit irCompiledUnit, CompilerContext compilerContext) {
        this.irCompiledUnit = irCompiledUnit;
        diagnostics = compilerContext.diagnostics;

        idToBlock = new Dictionary<int, BasicBlock>();
    }
    public IrCompiledUnit Analyse() {
        List<IrFunction> irFunctions = new();
        foreach (IrFunction irFunction in irCompiledUnit.irFunctions) {
            irFunctions.Add(AnalyseFunction(irFunction));
        }
        return new IrCompiledUnit(irFunctions.ToArray(), irCompiledUnit.mainFunctionInd);
    }

    public IrFunction AnalyseFunction(IrFunction irFunction) {
        BasicBlock[] blocks = irFunction.basicBlocks;
        FillIdToBlock(irFunction);

        var visited = new HashSet<int>();
        int head = 0;
        var worklist = new List<BasicBlock> { blocks[0] };

        while (head < worklist.Count) {
            BasicBlock currBlock = worklist[head++];

            if (!visited.Add(currBlock.blockId)) {
                continue;
            }

            switch (currBlock.terminator) {
                case IrReturn:
                    break;

                case IrGoto g: {
                        worklist.Add(idToBlock[g.basicBlockId]);
                        break;
                    }

                case IrBranch b: {
                        worklist.Add(idToBlock[b.thenBlockId]);
                        worklist.Add(idToBlock[b.elseBlockId]);
                        break;
                    }

                case null:
                    break;

                default:
                    throw new Exception("unkown terminator " + currBlock.terminator);
            }
        }

        var reached = new List<BasicBlock>(visited.Count);

        foreach (BasicBlock block in blocks) {
            if (visited.Contains(block.blockId)) {
                reached.Add(block);

                if (block.terminator is null) {
                    diagnostics.Report(block.location, DiagnosticDescriptors.ControlFLowAllPathsNeedReturn);
                }
            }
            else {
                diagnostics.Report(block.location, DiagnosticDescriptors.ConrolFlowUnreachableCode);
            }
        }
        idToBlock.Clear();
        return new IrFunction(reached.ToArray(), irFunction.localCount, irFunction.maxVReg, irFunction.paramCount);
    }

    void FillIdToBlock(IrFunction irFunction) {
        foreach (BasicBlock basic in irFunction.basicBlocks) {
            idToBlock[basic.blockId] = basic;
        }
    }
}
