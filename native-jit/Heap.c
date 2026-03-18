//
// Created by leona on 12.03.2026.
//

#include "Headers/Heap.h"

#include "Headers/Gc.h"

#include <stdio.h>
#include <stdlib.h>


#define ARENA_ALIGNMENT 8
#define ALIGN_UP(size) (((size) + (ARENA_ALIGNMENT - 1)) & ~(ARENA_ALIGNMENT - 1))

void InitHeap(Heap* heap) {
    heap->start = malloc(1024 * 1024);
    if (heap->start == NULL) {
        fprintf(stderr, "unable to alloc heap");
        exit(1);
    }
    heap->current = heap-> start;
    heap->end = heap->start + 1024 * 1024;
}

ObjHeader* AllocHeapObject(Heap* heap, u32 size) {
    u32 aligned = AlignSize(size);

    if (heap->current + aligned > heap->end) {
        return NULL;
    }

    ObjHeader* point = (ObjHeader*)heap->current;
    heap->current += aligned;

    return point;
}

u32 AlignSize(u32 size) {
    return ALIGN_UP(size);
}
void FreeHeap(Heap* heap) {
    free(heap->start);
}


