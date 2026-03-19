//
// Created by leona on 11.03.2026.
//

#include "Headers/VM.h"

#include "Headers/Gc.h"
#include "Headers/natives.h"
#include "Headers/value.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

CallFrame CreateFrame(Function* function,const u16 returnReg, const bool hasReturnReg) {
    CallFrame callFrame;
    callFrame.function = function;
    callFrame.instructionPointer = 0;
    callFrame.bytecodeOffset = 0;
    callFrame.returnReg = returnReg;
    callFrame.hasReturnReg = hasReturnReg;

    callFrame.regs = calloc(function->maxRegCount, sizeof(Value));
    if (callFrame.regs == NULL) {
        fprintf(stderr, "unable to alloc frame regs");
        exit(1);
    }

    if (function->localCount != 0) {
        callFrame.locals = calloc(function->localCount, sizeof(Value));
        if (callFrame.locals == NULL) {
            fprintf(stderr, "unable to alloc frame regs");
            exit(1);
        }
    } else {
        callFrame.locals = NULL;
    }

    return callFrame;
}

RuntimeTypeDesc* GetTypeDesc(Vm* vm, u32 id) {
    if (id >= vm->program->typeTableLength) {
        fprintf(stderr, "unkown type Id");
        exit(1);
    }

    RuntimeTypeDesc* type = &vm->program->typeTable[id];
    return type;
}
Value DefaultValueForType(RuntimeTypeDesc* type) {
    switch (type->kind) {
        case TYPE_INT: {
            Value val = {.type = VAL_INT, .rawData = 0};
            return val;
        }
        case TYPE_ARRAY: {
            Value val = {.type = VAL_NULL, .rawData = 0};
            return val;
        }
        case TYPE_BOOL: {
            Value val = {.type = VAL_BOOL, .rawData = 0};
            return val;
        }
        case TYPE_CHAR: {
            Value val = {.type = VAL_CHAR, .rawData = '\0'};
            return val;
        }
        case TYPE_OBJECT: {
            Value val = {.type = VAL_NULL, .rawData = 0};
            return val;
        }
        default: {
            fprintf(stderr, "unkown type in default value for type");
            exit(1);
        }
    }
}

ArrayObject* GetArrayObject(Vm* vm, Value arrayValue) {
    if (arrayValue.type == VAL_NULL) {
        fprintf(stderr, "array reference is null");
        exit(1);
    }

    ArrayObject* arrayObj = (ArrayObject*)AsHeapPointer(arrayValue);
    RuntimeTypeDesc* arrayType = GetTypeDesc(vm, arrayObj->header.typeId);
    if (arrayType->kind != TYPE_ARRAY || !arrayType->hasElementTypeId) {
        fprintf(stderr, "not pointing to an arrray bud");
        exit(1);
    }

    return arrayObj;
}

void SetArrayDefaultValues(ArrayObject* arrayObject, Value defaultValue) {
    for (u32 i = 0; i < arrayObject->length; i++) {
        arrayObject->elements[i] = defaultValue;
    }
}

ArrayObject* AllocateArrayObject(Vm* vm, i32 length, u32 typeId) {
    RuntimeTypeDesc* arrayType = GetTypeDesc(vm, typeId);

    if (arrayType->kind != TYPE_ARRAY || arrayType->hasElementTypeId == false) {
        fprintf(stderr, "allocating an array requires array type descriptor");
        exit(1);
    }

    u32 size = AlignSize(sizeof(ArrayObject) + (sizeof(Value) * length));
    ObjHeader objHeader = {.typeId = typeId, .size = size, .mark = false};

    ArrayObject* arrayObject = (ArrayObject*)AllocHeapObject(&vm->heap, size);
    if (arrayObject == NULL) {
        MarkAndSweep(vm);
        arrayObject = (ArrayObject*)AllocHeapObject(&vm->heap, size);
        if (arrayObject == NULL) {
            fprintf(stderr, "out of memory after GC");
            exit(1);
        }
    }

    arrayObject->header = objHeader;
    arrayObject->length = (u32)length;

    RuntimeTypeDesc* elementType = GetTypeDesc(vm, arrayType->elementTypeId);
    Value defVal = DefaultValueForType(elementType);
    SetArrayDefaultValues(arrayObject, defVal);

    return arrayObject;
}
void VmInit(Vm* vm, Program* program) {
    vm->program = program;
    vm->running = 1;

    NativeFn* resolved = resolveNativeFunctions(program);
    u16 nativeCount = program->nativeFunctionCount;
    vm->runtimeFunctions = malloc(sizeof(RuntimeFunction) * (nativeCount + program->functionCount));
    if (vm->runtimeFunctions == NULL) {
        fprintf(stderr, "unable to alloc runtime func");
        exit(1);
    }

    for (u16 i = 0; i < nativeCount; i++) {
        vm->runtimeFunctions[i].kind = FuncNative;
        vm->runtimeFunctions[i].nativeFn = resolved[i];
    }

    for (u16 i = 0; i < program->functionCount; i++) {
        vm->runtimeFunctions[i + nativeCount].kind = FuncUser;
        vm->runtimeFunctions[i + nativeCount].userFun = program->functions[i];
    }

    if (vm->runtimeFunctions[program->entryFunctionIndex].kind == FuncNative) {
        fprintf(stderr, "entry function was a native function");
        exit(1);
    }

    const CallFrame entryFrame = CreateFrame(&vm->runtimeFunctions[program->entryFunctionIndex].userFun, 0, false);
    vm->frames[0] = entryFrame;
    vm->depth = 0;
    Heap heap;
    InitHeap(&heap);
    vm->heap = heap;
}

void VmFree(Vm* vm) {
    for (i32 i = 0; i <= vm->depth; i++) {
        free(vm->frames[i].regs);
        free(vm->frames[i].locals);
    }
    free(vm->runtimeFunctions);
    FreeHeap(&vm->heap);
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

static u32 ReadU32(CallFrame* frame) {
    const u32 value = (u32)(frame->function->bytecode[frame->instructionPointer] |
                            (frame->function->bytecode[frame->instructionPointer + 1] << 8) |
                            (frame->function->bytecode[frame->instructionPointer + 2] << 16) |
                            (frame->function->bytecode[frame->instructionPointer + 3] << 24));
    frame->instructionPointer += 4;
    return value;
}

void CallFunction(Vm* vm, u16 dstReg, u32 functionIndex, u16* argRegs, u16 argCount) {
    if (vm->depth >= MAX_CALLS_DEPTH - 1) {
        fprintf(stderr, "max frame stack reached");
        exit(1);
    }
    CallFrame callFrame = CreateFrame(&vm->runtimeFunctions[functionIndex].userFun, dstReg, true);
    Value* callerRegs = vm->frames[vm->depth].regs;
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
        frame->bytecodeOffset = frame->instructionPointer;
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

                Value resValue = {.type = VAL_INT};

                resValue.rawData = AsInt(frame->regs[leftReg]) + AsInt(frame->regs[rightReg]);

                frame->regs[resReg] = resValue;
                break;
            }

            case SUB_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_INT};

                resValue.rawData = AsInt(frame->regs[leftReg]) - AsInt(frame->regs[rightReg]);

                frame->regs[resReg] = resValue;
                break;
            }

            case MULT_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_INT};

                resValue.rawData = AsInt(frame->regs[leftReg]) * AsInt(frame->regs[rightReg]);

                frame->regs[resReg] = resValue;
                break;
            }

            case DIV_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_INT};

                if (AsInt(frame->regs[rightReg]) == 0) {
                    fprintf(stderr, "tried to divide by 0");
                    exit(1);
                }
                resValue.rawData = AsInt(frame->regs[leftReg]) / AsInt(frame->regs[rightReg]);

                frame->regs[resReg] = resValue;
                break;
            }

            case MOD_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_INT};

                resValue.rawData = AsInt(frame->regs[leftReg]) % AsInt(frame->regs[rightReg]);

                frame->regs[resReg] = resValue;
                break;
            }

            case NEG_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 negReg = ReadU16(frame);

                Value resValue = {.type = VAL_INT};

                resValue.rawData = -AsInt(frame->regs[negReg]);

                frame->regs[resReg] = resValue;
                break;
            }

            case CMP_LT_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_BOOL};

                resValue.rawData = (AsInt(frame->regs[leftReg]) < AsInt(frame->regs[rightReg])) ? 1 : 0;

                frame->regs[resReg] = resValue;
                break;
            }

            case CMP_LTE_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_BOOL};

                resValue.rawData = (AsInt(frame->regs[leftReg]) <= AsInt(frame->regs[rightReg])) ? 1 : 0;

                frame->regs[resReg] = resValue;
                break;
            }

            case CMP_GT_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_BOOL};

                resValue.rawData = (AsInt(frame->regs[leftReg]) > AsInt(frame->regs[rightReg])) ? 1 : 0;

                frame->regs[resReg] = resValue;
                break;
            }

            case CMP_GTE_INT: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_BOOL};

                resValue.rawData = (AsInt(frame->regs[leftReg]) >= AsInt(frame->regs[rightReg])) ? 1 : 0;

                frame->regs[resReg] = resValue;
                break;
            }

            case NOT: {
                const u16 resReg = ReadU16(frame);
                const u16 notReg = ReadU16(frame);

                Value resValue = {.type = VAL_BOOL};

                resValue.rawData = AsBool(frame->regs[notReg]) ? 0 : 1;

                frame->regs[resReg] = resValue;
                break;
            }

            case JUMP: {
                const i32 offset = ReadI32(frame);
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

                Value resValue = {.type = VAL_BOOL};

                resValue.rawData = ((frame->regs[leftReg].rawData == frame->regs[rightReg].rawData) && (frame->regs[leftReg].type == frame->regs[rightReg].type)) ? 1 : 0;

                frame->regs[resReg] = resValue;
                break;
            }

            case CMP_NEQ: {
                const u16 resReg = ReadU16(frame);
                const u16 leftReg = ReadU16(frame);
                const u16 rightReg = ReadU16(frame);

                Value resValue = {.type = VAL_BOOL};

                resValue.rawData = ((frame->regs[leftReg].rawData != frame->regs[rightReg].rawData) || (frame->regs[leftReg].type != frame->regs[rightReg].type)) ? 1 : 0;

                frame->regs[resReg] = resValue;
                break;
            }

            case CALL: {
                const u16 dstReg = ReadU16(frame);
                const u32 functionIndex = ReadU32(frame);
                const u16 argCount = ReadU16(frame);
                u16 argRegs[argCount];
                for (u16 i = 0; i < argCount; i++) {
                    u16 argReg = ReadU16(frame);
                    argRegs[i] = argReg;
                }
                if (vm->runtimeFunctions[functionIndex].kind == FuncUser) {
                    CallFunction(vm, dstReg, functionIndex, argRegs, argCount);
                } else {
                    frame->regs[dstReg] = vm->runtimeFunctions[functionIndex].nativeFn(frame->regs, argCount, argRegs);
                }

                break;
            }

            case MOVE: {
                const u16 dstReg = ReadU16(frame);
                const u16 srcReg = ReadU16(frame);

                frame->regs[dstReg] = frame->regs[srcReg];
                break;
            }

            case NEW_ARRAY: {
                u16 dstReg = ReadU16(frame);
                u32 typeId = ReadU32(frame);
                u16 lengthReg = ReadU16(frame);

                i32 length = AsInt(frame->regs[lengthReg]);

                if (length < 0) {
                    fprintf(stderr, "tried to initialize array with negative length");
                    exit(1);
                }

                Value val = {.type = VAL_HEAPREF, .rawData = (i64)(intptr_t)AllocateArrayObject(vm, length, typeId)};
                frame->regs[dstReg] = val;
                break;
            }

            case LOAD_ARRAY: {
                u16 dstReg = ReadU16(frame);
                u16 arrayReg = ReadU16(frame);
                u16 indexReg = ReadU16(frame);

                Value arrayValue = frame->regs[arrayReg];

                i32 index = AsInt(frame->regs[indexReg]);
                ArrayObject* arrayObject = GetArrayObject(vm, arrayValue);

                if (index < 0 || (u32)index >= arrayObject->length) {
                    fprintf(stderr, "index out of bounds %d", index);
                    exit(1);
                }

                frame->regs[dstReg] = arrayObject->elements[index];
                break;
            }

            case STORE_ARRAY: {
                u16 srcReg = ReadU16(frame);
                u16 arrayReg = ReadU16(frame);
                u16 indexReg = ReadU16(frame);

                Value arrayValue = frame->regs[arrayReg];

                i32 index = AsInt(frame->regs[indexReg]);
                ArrayObject* arrayObject = GetArrayObject(vm, arrayValue);

                if (index < 0 || (u32)index >= arrayObject->length) {
                    fprintf(stderr, "index out of bounds %d", index);
                    exit(1);
                }

                arrayObject->elements[index] = frame->regs[srcReg];
                break;
            }

            case ARRAY_LENGTH: {
                u16 dstReg = ReadU16(frame);
                u16 arrayReg = ReadU16(frame);

                Value arrayValue = frame->regs[arrayReg];
                ArrayObject* arrayObject = GetArrayObject(vm, arrayValue);
                Value val = {.type = VAL_INT, .rawData = arrayObject->length};
                frame->regs[dstReg] = val;
                break;
            }

            default: {
                fprintf(stderr, "executing opcode: 0x%02X at ip: %u\n", opCode, frame->instructionPointer - 1);
                exit(1);
            }
        }
    }
    return (Value){.type = VAL_NULL, .rawData = 0};
}
