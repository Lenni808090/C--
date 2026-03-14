using System.Reflection.Emit;

namespace CMinus.Compiler.Lowering;

class StackMapAnalyser {







    RefUseDef GetRefUseDefTerminator(Terminator terminator, IrFunction irFunction) {
        var regIsRef = irFunction.regIsRef;

        bool[] usedRegs = new bool[irFunction.maxVReg];
        bool[] usedLocals = new bool[irFunction.localCount];
        bool[] definedRegs = new bool[irFunction.maxVReg];
        bool[] definedLocals = new bool[irFunction.localCount];

        switch (terminator) {
            case IrGoto: {
                    break;
                }
            case IrBranch branch: {
                    usedRegs[branch.condReg] = regIsRef[branch.condReg];
                    break;
                }
            case IrReturn @return: {
                    usedRegs[@return.returnReg] = regIsRef[@return.returnReg];
                    break;
                }
            default: {
                    throw new Exception("unknown terminator in stack analyser");
                }
        }

        return new RefUseDef(usedRegs, usedLocals, definedRegs, definedLocals);
    }

    RefUseDef GetRefUseInstr(IrInstr irInstr, IrFunction irFunction) {
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
            case IrLoadConst: {
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
                    definedRegs[newArray.dstReg] = regIsRef[newArray.dstReg];
                    break;
                }
            case IrArrayLength arrayLength: {
                    usedRegs[arrayLength.arrayReg] = regIsRef[arrayLength.arrayReg];
                    break;
                }
            case IrStoreElement storeElement: {
                    usedRegs[storeElement.arrayReg] = regIsRef[storeElement.arrayReg];
                    usedRegs[storeElement.srcReg] = regIsRef[storeElement.srcReg];
                    break;
                }
            case IrLoadElement loadElement: {
                    if (regIsRef[loadElement.dstReg]) {
                        definedRegs[loadElement.dstReg] = true;
                    }
                    usedRegs[loadElement.arrayReg] = regIsRef[loadElement.arrayReg];
                    break;
                }
            case IrMove move: {
                    usedRegs[move.srcReg] = regIsRef[move.srcReg];
                    definedRegs[move.dstReg] = regIsRef[move.dstReg];
                    break;
                }
            case IrBinaryOp binaryOp: {
                    usedRegs[binaryOp.leftReg] = regIsRef[binaryOp.leftReg];
                    usedRegs[binaryOp.rightReg] = regIsRef[binaryOp.rightReg];
                    break;
                }
            case IrUnary unary: {
                    definedRegs[unary.dstReg] = regIsRef[unary.dstReg];
                    usedRegs[unary.operandReg] = regIsRef[unary.operandReg];
                    break;
                }
            case IrCall call: {
                    definedRegs[call.dstReg] = regIsRef[call.dstReg];
                    foreach (int reg in call.argRegs) {
                        usedRegs[reg] = regIsRef[reg];
                    }
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
