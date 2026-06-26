//
// Created by leona on 26.06.2026.
//
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include "Headers/JitMemory.h"

#include <stdio.h>
#include <stdlib.h>

void InitJitMemBuffer(JitMemBuffer* jit_mem_buffer, u32 capacity) {
    void* memory = VirtualAlloc(
        NULL,
        capacity,
        MEM_COMMIT | MEM_RESERVE,
        PAGE_EXECUTE_READWRITE
        );
    if (memory == NULL) {
        fprintf(stderr, "failed to allocate jit memory\n");
        exit(1);
    }
    jit_mem_buffer->start = (u8*)memory;
    jit_mem_buffer->current = jit_mem_buffer->start;
    jit_mem_buffer->end = jit_mem_buffer->start + capacity;
}

void FreeJitMemBuffer(JitMemBuffer* jit_mem_buffer) {
    if (jit_mem_buffer->start == NULL) {
        return;
    }

    VirtualFree(jit_mem_buffer->start, 0, MEM_RELEASE);

    jit_mem_buffer->start = NULL;
    jit_mem_buffer->current = NULL;
    jit_mem_buffer->end = NULL;
}

u32 GetJitMemOffset(JitMemBuffer* jit_mem_buffer) {
    return (u32)(jit_mem_buffer->current - jit_mem_buffer->start);
}

void EmitByte(JitMemBuffer* jit_mem_buffer, u8 byte) {
    if (jit_mem_buffer->current + sizeof(u8) > jit_mem_buffer->end) {
        fprintf(stderr, "jit memory buffer overflown\n");
        exit(1);
    }

    *jit_mem_buffer->current = byte;
    jit_mem_buffer->current += sizeof(byte);
}

void EmitBytes(JitMemBuffer* jit_mem_buffer, u8* bytes, u32 count) {
    for (u32 i = 0; i < count; i++) {
        EmitByte(jit_mem_buffer, bytes[i]);
    }
}
