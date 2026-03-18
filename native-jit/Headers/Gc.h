//
// Created by leona on 18.03.2026.
//

#ifndef GC_H
#define GC_H
#include "VM.h"

void MarkAndSweep(Vm* vm);
u32 GetInstrInd(CallFrame* frame, FunctionStackMap* functionStack);
#endif // GC_H
