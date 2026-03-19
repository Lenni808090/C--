//
// Created by leona on 18.03.2026.
//

#ifndef GC_H
#define GC_H
#include "VM.h"

void MarkAndSweep(Vm* vm);
void MarkReg(u16 reg, CallFrame* frame,Vm* vm);
void MarkLocal(u16 local, CallFrame* frame,Vm* vm);
void MarkValue(Value val, Vm* vm);
void TraceObject(ObjHeader* obj, Vm* vm);

u32 GetDeadSpaceSize(u8** scanPointer, u8* endPointer);
StackMap GetStackMap(CallFrame* frame, FunctionStackMap* functionStack);

#endif // GC_H
