#ifndef TYPES_H
#define TYPES_H

#include <stdint.h>
#include  <stdbool.h>

typedef uint8_t u8;
typedef uint16_t u16;
typedef uint32_t u32;
typedef uint64_t u64;
typedef int8_t i8;
typedef int16_t i16;
typedef int32_t i32;
typedef int64_t i64;

typedef enum {
    VAL_INT,
    VAL_BOOL,
    VAL_CHAR,
    VAL_HEAPREF,
    VAL_NULL,
} ValueType;

typedef enum {
    TYPE_INT,
    TYPE_BOOL,
    TYPE_CHAR,
    TYPE_ARRAY,
    TYPE_OBJECT,
} RuntimeType;

typedef struct {
    ValueType type;
    i64 rawData;
} Value;

typedef struct {
    RuntimeType kind;
    i32 elementTypeId;
    bool hasElementTypeId;
    u16 nameLength;
    char* name;
} RuntimeTypeDesc;

typedef struct {
    u16 localCount;
    u16 paramCount;
    u16 maxRegCount;
    u32 bytecodeCount;
    u8* bytecode;
} Function;

typedef struct {
    u16 entryFunctionIndex;
    u16 functionCount;
    u16 typeTableLength;
    u16 constantCount;
    Function* functions;
    RuntimeTypeDesc* typeTable;
    Value* constants;
} Program;


#endif