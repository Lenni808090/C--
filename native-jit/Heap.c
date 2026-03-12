//
// Created by leona on 12.03.2026.
//

#include "Headers/Heap.h"

#include <stdio.h>
#include <stdlib.h>

void InitHeap(Heap* heap, u32 capacity) {
    heap->objects = malloc(sizeof(HeapObject*) * capacity);
    heap->heapLength = 0;
    heap->capacity = capacity;
}

int AllocHeapObject(Heap* heap, HeapObject* heapObject) {
    if (heap->heapLength >= heap->capacity) {
        HeapObject** tempHeap = realloc(heap->objects, sizeof(HeapObject*) * heap->capacity * 2);
        if (tempHeap == NULL) {
            fprintf(stderr, "unable to realloc heap objects");
            exit(1);
        }
        heap->capacity *= 2;
        heap->objects = tempHeap;
    }
    heap->objects[heap->heapLength] = heapObject;
    return heap->heapLength++;
}

HeapObject* GetHeapObject(Heap* heap,u32 id) {
    return heap->objects[id];
}

void FreeHeap(Heap* heap) {
    free(heap->objects);
}


