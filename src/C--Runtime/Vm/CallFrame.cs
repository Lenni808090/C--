
namespace CMinus.Runtime;

class CallFrame {
    public int instructionPointer = 0;
    public Value[] regs;
    public Value[] locals;
    public int functionInd;
    public ushort? returnReg;

    public CallFrame(Value[] regs, Value[] locals, ushort? returnReg, int functionInd) {
        this.functionInd = functionInd;
        this.regs = regs;
        this.locals = locals;
        this.returnReg = returnReg;
    }

}