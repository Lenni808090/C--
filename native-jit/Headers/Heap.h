//
// Created by leona on 12.03.2026.
//

#ifndef HEAP_H
#define HEAP_H


#include "types.h"

struct Vm;

typedef enum {
    BlockFree,
    BlockObject,
} BlockKind;



typedef struct {
    u32 typeId;
    u32 size;
    BlockKind kind;
    bool mark;
} ObjHeader;

typedef struct {
    ObjHeader header;
    u32 length;
    Value elements[];
} ArrayObject;

typedef struct FreeBlock {
    ObjHeader base;
    struct FreeBlock* next;
} FreeBlock;

typedef struct {
    u8* start;
    u8* end;
    u8* current;
    FreeBlock* freeList;
} Heap;

void InitHeap(struct Vm* vm);
ObjHeader* AllocHeapObject(struct Vm* vm, u32 size);
void FreeHeap(struct Vm* vm);
u32 AlignSize(u32 size);
ObjHeader* TryBumpAlloc(struct Vm* vm, u32 alignedSize) ;
ObjHeader* TryFeeListAlloc( struct Vm* vm, u32 size);
#endif // HEAP_H
