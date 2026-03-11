//
// Created by leona on 11.03.2026.
//

#include "Headers/VM.h"

#include "Headers/value.h"

#include <stdio.h>
#include <stdlib.h>

CallFrame CreateFrame(const Function* function, const u16 returnReg, const bool hasReturnReg) {
    CallFrame callFrame;
    callFrame.function = function;
    callFrame.instructionPointer = 0;
    callFrame.returnReg = returnReg;
    callFrame.hasReturnReg = hasReturnReg;

    callFrame.regs = malloc(sizeof(Value) * function->maxRegCount);
    if (callFrame.regs == NULL) {
        fprintf(stderr, "unable to alloc frame regs");
        exit(1);
    }


    callFrame.locals = malloc(sizeof(Value) * function->localCount);
    if (callFrame.locals == NULL) {
        fprintf(stderr, "unable to alloc frame locals");
        exit(1);
    }

    return callFrame;
}

void VmInit(Vm* vm, const Program* program) {
    vm->program = program;
    vm->running = 1;
    const CallFrame entryFrame = CreateFrame(&program->functions[program->entryFunctionIndex], 0, false);
    vm->frames[0] = entryFrame;
    vm->depth = 0;
}

void VmFree(const Vm* vm) {
    for (i32 i = 0; i <= vm->depth; i++) {
        free(vm->frames[i].regs);
        free(vm->frames[i].locals);
    }
}

static u16 ReadU16(CallFrame* frame) {
    const u16 value = (u16)(frame->function->bytecode[frame->instructionPointer] |
                     (frame->function->bytecode[frame->instructionPointer + 1] << 8));
    frame->instructionPointer += 2;
    return value;
}

  static i32 ReadI32(CallFrame* frame) {
     const i32 value = (i32)(frame->function->bytecode[frame->instructionPointer] |
                      (frame->function->bytecode[frame->instructionPointer + 1] << 8) |
                      (frame->function->bytecode[frame->instructionPointer + 2] << 16) |
                       (frame->function->bytecode[frame->instructionPointer + 3] << 24));
     frame->instructionPointer += 4;
      return value;
 }

void CallFunction(Vm* vm, u16 dstReg, i32 functionIndex, u16* argRegs, u16 argCount) {
    const CallFrame callFrame = CreateFrame(&vm->program->functions[functionIndex], dstReg, true);
    const Value* callerRegs = vm->frames[vm->depth].regs;
    for (u16 i = 0; i < argCount; i++) {
        const Value param = callerRegs[argRegs[i]];
        callFrame.locals[i] = param;
    }
    vm->depth++;
    vm->frames[vm->depth] = callFrame;
}

Value VmRun(Vm* vm) {

    while (vm->running) {
        CallFrame* frame = &vm->frames[vm->depth];
        const u8 opCode = frame->function->bytecode[frame->instructionPointer++];

        switch (opCode) {
            case LOAD_CONST: {
                const u16 dstReg = ReadU16(frame);
                const u16 constInd = ReadU16(frame);

                frame->regs[dstReg] = vm->program->constants[constInd];
                break;
            }

            case STORE_LOCAL: {
                const u16 srcReg = ReadU16(frame);
                const u16 localIndex = ReadU16(frame);

                frame->locals[localIndex] = frame->regs[srcReg];
                break;
            }

            case LOAD_LOCAL: {
                const u16 dstReg = ReadU16(frame);
                const u16 localIndex = ReadU16(frame);

                frame->regs[dstReg] = frame->locals[localIndex];
                break;
            }

            case RETURN: {
                const u16 returnedReg = ReadU16(frame);
                const Value returnedVal = frame->regs[returnedReg];
                if (!frame->hasReturnReg) {
                    vm->running = false;
                    return returnedVal;
                }
                if (vm->depth != 0) {
                    const u16 returnReg = frame->returnReg;
                    free(frame->regs);
                    free(frame->locals);
                    vm->depth--;

                    CallFrame* callerFrame = &vm->frames[vm->depth];
                    callerFrame->regs[returnReg] = returnedVal;
                }
                break;
            }
            case ADD_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_INT };

                resValue.rawData = AsInt(frame->regs[leftReg]) + AsInt(frame->regs[rightReg]);

                frame->regs[resReg] = resValue;
                break;
            }

            case SUB_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_INT };

                resValue.rawData = AsInt(frame->regs[leftReg]) - AsInt(frame->regs[rightReg]);

                frame->regs[resReg] = resValue;
                break;
            }

            case MULT_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_INT };

                resValue.rawData = AsInt(frame->regs[leftReg]) * AsInt(frame->regs[rightReg]);

                frame->regs[resReg] = resValue;
                break;
            }

            case DIV_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_INT };

                resValue.rawData = AsInt(frame->regs[leftReg]) / AsInt(frame->regs[rightReg]);

                frame->regs[resReg] = resValue;
                break;
            }

            case MOD_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_INT };

                resValue.rawData = AsInt(frame->regs[leftReg]) % AsInt(frame->regs[rightReg]);

                frame->regs[resReg] = resValue;
                break;
            }

            case NEG_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 negReg = ReadU16(frame);

                Value resValue = {.type = VAL_INT };

                resValue.rawData = -AsInt(frame->regs[negReg]);

                frame->regs[resReg] = resValue;
                break;
            }

            case  CMP_LT_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_BOOL };

                resValue.rawData = (AsInt(frame->regs[leftReg]) < AsInt(frame->regs[rightReg])) ? 1 : 0;

                frame->regs[resReg] = resValue;
                break;
            }

            case CMP_LTE_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_BOOL };

                resValue.rawData = (AsInt(frame->regs[leftReg]) <= AsInt(frame->regs[rightReg])) ? 1 : 0;

                frame->regs[resReg] = resValue;
                break;
            }

            case CMP_GT_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_BOOL };

                resValue.rawData = (AsInt(frame->regs[leftReg]) > AsInt(frame->regs[rightReg])) ? 1 : 0;

                frame->regs[resReg] = resValue;
                break;
            }

            case CMP_GTE_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_BOOL };

                resValue.rawData = (AsInt(frame->regs[leftReg]) <= AsInt(frame->regs[rightReg])) ? 1 : 0;

                frame->regs[resReg] = resValue;
                break;
            }

            case NOT: {
                const u16 resReg = ReadU16(frame);
                const u16 notReg = ReadU16(frame);

                Value resValue = {.type = VAL_BOOL };

                resValue.rawData = AsBool(frame->regs[notReg]) ? 0 : 1;

                frame->regs[resReg] = resValue;
                break;
            }

            case JUMP: {
                const u32 offset = ReadI32(frame);
                frame->instructionPointer += offset;
                break;
            }

            case JUMP_IF_FALSE: {
                const u16 condReg = ReadU16(frame);
                const i32 offset = ReadI32(frame);
                bool cond = AsBool(frame->regs[condReg]);
                if (!cond) {
                    frame->instructionPointer += offset;
                }
                break;
            }

            case JUMP_IF_TRUE: {
                const u16 condReg = ReadU16(frame);
                const i32 offset = ReadI32(frame);
                bool cond = AsBool(frame->regs[condReg]);
                if (cond) {
                    frame->instructionPointer += offset;
                }
                break;
            }

            case CMP_EQ: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_BOOL };

                resValue.rawData = ((frame->regs[leftReg].rawData == frame->regs[rightReg].rawData) && (frame->regs[leftReg].type == frame->regs[rightReg].type)) ? 1 : 0;

                frame->regs[resReg] = resValue;
                break;
            }

            case CMP_NEQ: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_BOOL };

                resValue.rawData = ((frame->regs[leftReg].rawData != frame->regs[rightReg].rawData) || (frame->regs[leftReg].type != frame->regs[rightReg].type)) ? 1 : 0;

                frame->regs[resReg] = resValue;
                break;
            }


            case CALL: {
                const u16 dstReg = ReadU16(frame);
                const i32 functionIndex = ReadI32(frame);
                const u16 argCount = ReadU16(frame);
                u16 argRegs[argCount];
                for (u16 i = 0; i < argCount; i++) {
                    u16 argReg = ReadU16(frame);
                    argRegs[i] = argReg;
                }
            }

            default: {
                fprintf(stderr, "executing opcode: 0x%02X at ip: %u\n", opCode, frame->instructionPointer - 1);
                exit(1);
            }
        }
    }
    return (Value){.type = VAL_NULL, .rawData = 0};;
}



































