//
// Created by leona on 13.03.2026.
//

#include "Headers/arena.h"

#include <stdio.h>
#include <stdlib.h>

#define ARENA_ALIGNMENT 8
#define ALIGN_UP(size) (((size) + (ARENA_ALIGNMENT - 1)) & ~(ARENA_ALIGNMENT - 1))

void ArenaInit(Arena* arena, size_t capacity) {
    arena->base = malloc(capacity);
    if (arena->base == NULL) {
        fprintf(stderr, "unable to alloc arena");
        exit(1);
    }
    arena->current = arena->base;
    arena->end = arena->base + capacity;
}

void* ArenaAlloc(Arena* arena, size_t size) {
    size_t aligned = ALIGN_UP(size);

    if (arena->current + aligned > arena->end) {
        fprintf(stderr, "arena out of memory: requested %zu, remaining %zu\n", size, (size_t)(arena->end - arena->current));
        exit(1);
    }

    void* ptr = arena->current;
    arena->current += aligned;
    return ptr;
}

void ArenaReset(Arena* arena) {
    arena->current = arena->base;
}

void FreeArena(Arena* arena) {
    free(arena->base);
    arena->base = NULL;
    arena->current = NULL;
    arena->end = NULL;
}