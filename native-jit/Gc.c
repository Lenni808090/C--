//
// Created by leona on 18.03.2026.
//

#include "Headers/Gc.h"

#include <stdio.h>
#include <stdlib.h>

void MarkAndSweep(Vm* vm) {
    (void)vm;
}

u32 GetInstrInd(CallFrame* frame, FunctionStackMap* functionStack) {
    if (functionStack->stackMapCount == 0) {
        fprintf(stderr, "no stack maps available for frame");
        exit(1);
    }

    u32 low = 0;
    u32 high = functionStack->stackMapCount;

    while (low < high) {
        u32 foundInd = low + ((high - low) / 2);
        u32 byteoffset = functionStack->stackMaps[foundInd].byteoffset;

        if (byteoffset == frame->bytecodeOffset) {
            return foundInd;
        }

        if (byteoffset < frame->bytecodeOffset) {
            low = foundInd + 1;
        } else {
            high = foundInd;
        }
    }

    fprintf(stderr, "no stack map for bytecode offset %u", frame->bytecodeOffset);
    exit(1);
}
