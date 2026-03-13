//
// Created by leona on 13.03.2026.
//

#ifndef ARENA_H
#define ARENA_H

#include <stddef.h>

#include "Headers/types.h"

typedef struct {
    u8* base;
    u8* current;
    u8* end;
} Arena;

void ArenaInit(Arena* arena, size_t capacity);
void* ArenaAlloc(Arena* arena,size_t size);
void ArenaReset(Arena* arena);
void FreeArena(Arena* arena);

#endif // ARENA_H
