//
// Created by leona on 12.03.2026.
//

#include "Headers/Heap.h"

#include "Headers/Gc.h"

#include <stdio.h>
#include <stdlib.h>


#define ARENA_ALIGNMENT 8
#define ALIGN_UP(size) (((size) + (ARENA_ALIGNMENT - 1)) & ~(ARENA_ALIGNMENT - 1))

void InitHeap(Vm* vm) {
    Heap* heap = &vm->heap;
    heap->start = malloc(1024 * 1024);
    if (heap->start == NULL) {
        fprintf(stderr, "unable to alloc heap");
        exit(1);
    }
    heap->current = heap-> start;
    heap->end = heap->start + 1024 * 1024;
    heap->freeList = NULL;
}

ObjHeader* AllocHeapObject(Vm* vm, u32 size) {
    u32 aligned = AlignSize(size);
    ObjHeader* point;

    point = TryBumpAlloc(vm, aligned);
    if (point) {
        return point;
    }

    point = TryFeeListAlloc(vm, aligned);
    if (point) {
        return point;
    }

    MarkAndSweep(vm);

    point = TryBumpAlloc(vm, aligned);
    if (point) {
        return point;
    }

    return TryFeeListAlloc(vm, aligned);
}

ObjHeader* TryBumpAlloc(Vm* vm, u32 alignedSize) {
    Heap* heap = &vm->heap;
    ObjHeader* point = (ObjHeader*)heap->current;

    if (heap->current + alignedSize > heap->end) {
       return NULL;
    }

    heap->current += alignedSize;

    return point;
}

ObjHeader* TryFeeListAlloc(Vm* vm, u32 size) {
    Heap* heap = &vm->heap;
    if (heap->freeList == NULL) {
        return NULL;
    }

    FreeBlock* prev = NULL;
    FreeBlock* currBlock = heap->freeList;

    while (true) {
        if (currBlock->base.size >= size) {
            // if i am on the first block and it fits reset the first one and dont get null pointered bang
            if (prev == NULL) {
                heap->freeList = currBlock->next;
            }else {
                prev->next = currBlock->next;
            }
            return (u8*)currBlock;
        }

        if (currBlock->next == NULL) {
            return NULL;
        }

        prev = currBlock;
        currBlock = currBlock->next;
    }
}

u32 AlignSize(u32 size) {
    return ALIGN_UP(size);
}

void FreeHeap(Vm* vm) {
    Heap* heap = &vm->heap;
    free(heap->start);
}


