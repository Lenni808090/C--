//
// Created by leona on 11.03.2026.
//

#ifndef VM_H
#define VM_H

#include "types.h"
#include "Heap.h"
#include <stdbool.h>

enum OpCodes {

    LOAD_CONST = 0x00,
    LOAD_LOCAL,
    STORE_LOCAL,

    RETURN,

    ADD_INT,
    SUB_INT,
    MULT_INT,
    DIV_INT,
    MOD_INT,
    NEG_INT,

    NOT,

    JUMP,
    JUMP_IF_FALSE,
    JUMP_IF_TRUE,


    CMP_LT_INT,
    CMP_LTE_INT,
    CMP_GT_INT,
    CMP_GTE_INT,

    CMP_EQ,
    CMP_NEQ,

    NEW_ARRAY,
    STORE_ARRAY,
    LOAD_ARRAY,
    ARRAY_LENGTH,

    MOVE,

    CALL,
};


typedef struct {
    const Function* function;
    u32 instructionPointer;
    Value* regs;
    bool hasReturnReg;
    u16 returnReg;
    Value* locals;
}CallFrame;

typedef struct {
    const Program* program;
    CallFrame frames[256];
    Heap heap;
    i32 depth;
    bool running;
} Vm;

void CallFunction(Vm* vm,u16 dstReg, i32 functionIndex, u16* argRegs, u16 argCount);
CallFrame CreateFrame(const Function* function, u16 returnReg, bool hasReturnReg);
i32 AllocateArrayObject(Vm* vm,i32 length, i32 typeId);
Value DefaultValueForType(RuntimeTypeDesc* type);
RuntimeTypeDesc* GetTypeDesc(Vm* vm,int id);
void setArrayDefaultValues(Vm* vm, ArrayObject* arrayObject);
void VmInit(Vm* vm, const Program* program);
void VmFree(Vm* vm);
Value VmRun(Vm* vm);

#endif