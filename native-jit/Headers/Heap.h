//
// Created by leona on 12.03.2026.
//

#ifndef HEAP_H
#define HEAP_H


#include "types.h"


typedef struct {
    u32 typeId;
    u32 size;
    bool mark;
} ObjHeader;

typedef struct {
    ObjHeader header;
    u32 length;
    Value elements[];
} ArrayObject;

typedef struct {
    u8* start;
    u8* end;
    u8* current;
} Heap;

void InitHeap(Heap* heap);
ObjHeader* AllocHeapObject(Heap* heap,u32 size);
void FreeHeap(Heap* heap);
u32 AlignSize(u32 size);
#endif // HEAP_H
