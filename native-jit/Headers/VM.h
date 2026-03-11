//
// Created by leona on 11.03.2026.
//

#ifndef VM_H
#define VM_H

#include "types.h"
#include <stdbool.h>

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
    i32 depth;
    u8 running;
} Vm;

CallFrame CreateFrame(const Function* function, u16 returnReg);
void VmInit(Vm* vm, const Program* program);
void VmFree(const Vm* vm);
Value VmRun(Vm* vm);

#endif