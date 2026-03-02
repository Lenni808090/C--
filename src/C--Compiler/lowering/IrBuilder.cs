using CMinus.Compiler.Binding;
using CMinus.Compiler.Lowering;

class IrBuilder {
    List<BasicBlock> basicBlocks;
    BasicBlock currentBlock;
    int nextBlockId;
    int nextVReg;
    int maxVReg;

    public IrBuilder() {
        basicBlocks = new();
        currentBlock = MakeNewBlock();
        basicBlocks.Add(currentBlock);
    }


    public IrCompiledUnit BuildCompiledUnit(BoundCompiledUnit boundCompiledUnit) {
        var stmts = boundCompiledUnit.boundStmts;

        foreach (var stmt in stmts) {
            BuildStmt(stmt);
        }

        return new IrCompiledUnit(basicBlocks.ToArray(), boundCompiledUnit.localCount, maxVReg);
    }

    void BuildStmt(BoundStmt boundStmt) {
        switch (boundStmt) {


            default: {
                    break;
                }
        }
    }


    int BuildExpr(BoundExpr boundExpr) {
        switch (boundExpr) {


            default: {
                    break;
                }
        }
    }

    void Emit(IrInstr irInstr) {
        currentBlock.irInstrs.Add(irInstr);
    }

    void Terminate(Terminator terminator) {
        if (currentBlock.terminator is not null) {
            throw new Exception("onyl one terminator allowed");
        }
        currentBlock.terminator = terminator;
    }


    int GetBlockId() {
        return nextBlockId++;
    }

    BasicBlock MakeNewBlock() {
        return new BasicBlock(GetBlockId());
    }



    int AllocVReg() {
        int reg = nextVReg++;
        if (nextVReg > maxVReg) {
            maxVReg = nextVReg;
        }
        return reg;
    }

}