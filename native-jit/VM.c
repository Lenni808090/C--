//
// Created by leona on 11.03.2026.
//

#include "Headers/VM.h"


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
    for (i32 i = 0; i < vm->depth; i++) {
        free(vm->frames[i].regs);
        free(vm->frames[i].locals);
    }
}

Value VmRun(Vm* vm) {


}
