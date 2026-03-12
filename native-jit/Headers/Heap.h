//
// Created by leona on 12.03.2026.
//

#ifndef HEAP_H
#define HEAP_H
#include "VM.h"
#include "types.h"

typedef enum {
    ArrayObjectKind,
} HeapObjectKind;

typedef struct {
    HeapObjectKind heapObjectKind;
}HeapObject;

typedef struct {
    HeapObject base;
    RuntimeType kind;
    i32 elementTypeId;
    u32 length;
    Value* elements;
}ArrayObject;

typedef struct {
    HeapObject** objects;
    u32 heapLength;
    u32 capacity;
} Heap;

void InitHeap(Heap* heap, u32 capacity);
int AllocHeapObject(Heap* heap, HeapObject* heapObject);
HeapObject* GetHeapObject(Heap* heap,u32 id);
void FreeHeap(Heap* heap);

#endif // HEAP_H
