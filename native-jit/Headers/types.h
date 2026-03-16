#ifndef TYPES_H
#define TYPES_H

#include "basetypes.h"
#include "arena.h"

#include <stdbool.h>


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
typedef void (*NativeFn)(Value* registers, u16 argCount, u16* argRegs);

typedef enum {
    FuncNative,
    FuncUser,
}FunctionKind;

typedef struct {
    FunctionKind kind;
    union {
        NativeFn nativeFn;
        Function userFun;
    };
} RuntimeFunction;

typedef struct {
    i32 byteoffset;
    u64* liveRegs;
    u64* liveLocals;
} StackMap;

typedef struct {
    i32 stackMapCount;
    u16 regWordCount;
    u16 localWordCount;
    StackMap* stackMaps;
} FunctionStackMap;

typedef struct {
    Arena arena;
    u16 entryFunctionIndex;
    u16 functionCount;
    u16 typeTableLength;
    u16 constantCount;
    u16 nativeFunctionCount;
    Function* functions;
    RuntimeTypeDesc* typeTable;
    FunctionStackMap* functionStackMaps;
    Value* constants;
    char** nativeFunctionNames;
} Program;


#endif
