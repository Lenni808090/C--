//
// Created by leona on 26.06.2026.
//

#ifndef NATIVE_JIT_JITMEMORY_H
#define NATIVE_JIT_JITMEMORY_H
#include "basetypes.h"

typedef struct {
    u8* start;
    u8* current;
    u8* end;
} JitMemBuffer;


void InitJitMemBuffer(JitMemBuffer* jit_mem_buffer, u32 capacity);
void FreeJitMemBuffer(JitMemBuffer* jit_mem_buffer);
u32 GetJitMemOffset(JitMemBuffer* jit_mem_buffer);
void EmitByte(JitMemBuffer* jit_mem_buffer, u8 byte);
void EmitBytes(JitMemBuffer* jit_mem_buffer, u8* bytes, u32 count);

#endif //NATIVE_JIT_JITMEMORY_H
