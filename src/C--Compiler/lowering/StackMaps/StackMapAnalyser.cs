using System.Reflection.Emit;

namespace CMinus.Compiler.Lowering;

class StackMapAnalyser {


    RefUseDef GetUseRef(IrInstr irInstr, IrFunction irFunction) {
        var regIsRef = irFunction.regIsRef;
        var localIsRef = irFunction.localIsRef;
        bool[] usedRegs = new bool[irFunction.maxVReg];
        bool[] usedLocals = new bool[irFunction.localCount];
        bool[] definedRegs = new bool[irFunction.maxVReg];
        bool[] definedLocals = new bool[irFunction.localCount];

        switch (irInstr) {
            case IrStoreLocal storeLocal: {
                    if (localIsRef[storeLocal.localIndex]) {
                        definedLocals[storeLocal.localIndex] = true;
                        usedRegs[storeLocal.srcReg] = true;
                    }
                    break;
                }
            case IrLoadLocal loadLocal: {
                    if (localIsRef[loadLocal.localIndex]) {
                        usedLocals[loadLocal.localIndex] = true;
                        definedRegs[loadLocal.dstReg] = true;
                    }
                    break;
                }
            case IrNewArray newArray: {
                    definedRegs[newArray.dstReg] = true;
                    break;
                }
            case IrArrayLength arrayLength: {
                    usedRegs[arrayLength.arrayReg] = true;
                    break;
                }
            case IrStoreElement storeElement: {
                    usedRegs[storeElement.arrayReg] = true;
                    break;
                }
            case IrLoadElement loadElement: {
                    if (regIsRef[loadElement.dstReg]) {
                        definedRegs[loadElement.dstReg] = true;
                    }
                    usedRegs[loadElement.arrayReg] = true;
                    break;
                }
            case IrMove move: {
                    if (regIsRef[move.srcReg]) {
                        definedRegs[move.dstReg] = true;
                        usedRegs[move.srcReg] = true;
                    }
                    break;
                }
            case IrBinaryOp binaryOp: {
                    usedRegs[binaryOp.leftReg] = regIsRef[binaryOp.leftReg];
                    usedRegs[binaryOp.rightReg] = regIsRef[binaryOp.rightReg];
                    break;
                }
            case IrUnary unary: {
                    if (regIsRef[unary.operandReg]) {
                        definedRegs[unary.dstReg] = true;
                        usedRegs[unary.operandReg] = true;
                    }
                    break;
                }
            case IrCall call: {
                    definedRegs[call.dstReg] = regIsRef[call.dstReg];
                    break;
                }
            default: {
                    throw new Exception("unknown ir instruction in stack map analyser");
                }
        }

        return new RefUseDef(usedRegs, usedLocals, definedRegs, definedLocals);
    }
}

struct RefUseDef {
    bool[] usedRegs;
    bool[] usedLocals;
    bool[] definedRegs;
    bool[] definedLocals;

    public RefUseDef(bool[] usedRegs, bool[] usedLocals, bool[] definedRegs, bool[] definedLocals) {
        this.usedLocals = usedLocals;
        this.usedRegs = usedRegs;
        this.definedLocals = definedLocals;
        this.definedRegs = definedRegs;
    }
}
