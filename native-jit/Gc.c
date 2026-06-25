//
// Created by leona on 18.03.2026.
//

#include "Headers/Gc.h"

#include <stdio.h>
#include <stdlib.h>

#ifdef _MSC_VER
#include <intrin.h>
#endif

static u16 CountTrailingZeros64(u64 value) {
#ifdef _MSC_VER
    unsigned long index;
    _BitScanForward64(&index, value);
    return (u16)index;
#else
    return (u16)__builtin_ctzll(value);
#endif
}

void MarkAndSweep(Vm* vm) {
    for (i32 i = vm->depth;i >= 0; i--) {
        CallFrame* frame = &vm->frames[i];
        FunctionStackMap* functionStackMap= &frame->function->functionStackMap;
        StackMap stackMap = GetStackMap(frame, functionStackMap);
        for (u16 r = 0; r < functionStackMap->regWordCount; r++) {
            u64 mask = stackMap.liveRegs[r];
            while (mask != 0) {
                u16 bitPos = CountTrailingZeros64(mask);
                u16 reg = r * 64 + bitPos;
                MarkReg(reg, frame, vm);
                //funny trick.
                //for example. 0b100 is 4,
                //take - 1 and that's 0b011
                //now if u do &= everything at and below the lowest 1 gets set to 0;
                mask &= mask - 1;
            }
        }

        for (u16 l = 0; l < functionStackMap->localWordCount; l++) {
            u64 mask = stackMap.liveLocals[l];
            while (mask != 0) {
                u16 bitPos = CountTrailingZeros64(mask);
                u16 local = l * 64 + bitPos;
                MarkLocal(local, frame, vm);
                mask &= mask - 1;
            }
        }
    }

    u8* scan = vm->heap.start;
    u8* sweepMax = vm->heap.current;
    vm->heap.freeList = NULL;
    while (scan < sweepMax) {
        ObjHeader* currObject = (ObjHeader*)scan;
        if (currObject->mark) {
            currObject->mark = false;
            scan += currObject->size;
        } else {
            u32 size = GetDeadSpaceSize(&scan, sweepMax);
            //creates pointer to heap to not loose it
            FreeBlock* freed = (FreeBlock*)currObject;
            freed->base.kind = BlockFree;
            freed->base.size = size;
            freed->next = vm->heap.freeList;
            vm->heap.freeList = freed;
        }
    }
}

u32 GetDeadSpaceSize(u8** scanPointer, u8* endPointer) {
    u32 totalSize = ((ObjHeader*)*scanPointer)->size;
    u8* next = *scanPointer + totalSize;

    while (next < endPointer) {
        ObjHeader* nextObj = (ObjHeader*)next;
        if (nextObj->mark || nextObj->kind == BlockFree) {
            break;
        }
        totalSize += nextObj->size;
        next += nextObj->size;
    }

    *scanPointer = next;
    return totalSize;
}

void MarkReg(u16 reg, CallFrame* frame,Vm* vm) {
    Value regVal = frame->regs[reg];
    MarkValue(regVal, vm);
}

void MarkLocal(u16 local, CallFrame* frame,Vm* vm) {
    Value localVal = frame->locals[local];
    MarkValue(localVal, vm);
}

void MarkValue(Value val, Vm* vm){
    if (val.type != VAL_HEAPREF){
        return;
    }

    ObjHeader* objHeader = (ObjHeader*)(intptr_t)val.rawData;
    if (objHeader->mark){
        return;
    }

    objHeader->mark = true;
    TraceObject(objHeader, vm);
}

void TraceObject(ObjHeader* obj, Vm* vm) {
    RuntimeTypeDesc* type = GetTypeDesc(vm, obj->typeId);
    switch (type->kind) {
        case TYPE_ARRAY: {
            RuntimeTypeDesc* elementType = GetTypeDesc(vm, type->elementTypeId);

            if (elementType->kind != TYPE_ARRAY && elementType->kind != TYPE_OBJECT) {
                break;
            }

            ArrayObject* arrayObject = (ArrayObject*)obj;
            for (u32 i = 0; i < arrayObject->length; i++) {
                MarkValue(arrayObject->elements[i], vm);
            }
            break;
        }

        default: {
            fprintf(stderr, "unkown object top trace");
            exit(1);
        }
    }
}

StackMap GetStackMap(CallFrame* frame, FunctionStackMap* functionStack) {
    if (functionStack->stackMapCount == 0) {
        fprintf(stderr, "no stack maps available for frame");
        exit(1);
    }

    u32 low = 0;
    u32 high = functionStack->stackMapCount;

    while (low < high) {
                        // makes it so no overlflow happens
        u32 foundInd = low + ((high - low) / 2);
        u32 byteoffset = functionStack->stackMaps[foundInd].byteoffset;

        if (byteoffset == frame->bytecodeOffset) {
            return functionStack->stackMaps[foundInd];
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
