#include "Headers/loader.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include <inttypes.h>
static const u8 MAGIC_NUMBER[] = {0x43, 0x4D, 0x4D, 0x00};

Program LoadProgam(const char* path) {
    FILE* file = fopen(path, "rb");

    fseek(file, 0, SEEK_END);
    i64 byteSize = ftell(file);
    fseek(file, 0, SEEK_SET);

    if (file == NULL) {
        fprintf(stderr, "could not open file: %s\n", path);
        exit(1);
    }

    u8 magic[4];
    fread(magic, 1, 4, file);

    if (memcmp(magic, MAGIC_NUMBER, 4) != 0) {
        fprintf(stderr, "invalid magic number\n");
        exit(1);
    }
    Program program;

    Arena arena;
    ArenaInit(&arena, byteSize * 4);
    program.arena = arena;

    u16 version;
    fread(&version, sizeof(u16), 1, file);

    fread(&program.entryFunctionIndex, sizeof(u16), 1, file);
    fread(&program.functionCount, sizeof(u16), 1, file);
    fread(&program.typeTableLength, sizeof(u16), 1, file);
    fread(&program.constantCount, sizeof(u16), 1, file);
    fread(&program.nativeFunctionCount, sizeof(u16), 1, file);

    program.typeTable = ArenaAlloc(&program.arena, sizeof(RuntimeTypeDesc) * program.typeTableLength);

    for (u16 i = 0; i < program.typeTableLength; i++) {
        u8 kind;
        fread(&kind, sizeof(u8), 1, file);
        program.typeTable[i].kind = kind;

        i32 elementTypeId;
        fread(&elementTypeId, sizeof(i32), 1, file);

        if (elementTypeId == 0x7FFFFFFF) {
            program.typeTable[i].hasElementTypeId = false;
        } else {
            program.typeTable[i].hasElementTypeId = true;
        }

        program.typeTable[i].elementTypeId = elementTypeId;

        u16 nameLength;
        fread(&nameLength, sizeof(u16), 1, file);
        program.typeTable[i].nameLength = nameLength;

        if (nameLength > 0) {
            program.typeTable[i].name = ArenaAlloc(&program.arena, sizeof(char) * (nameLength + 1));
            fread(program.typeTable[i].name, sizeof(char), nameLength, file);
            program.typeTable[i].name[nameLength] = '\0';
        } else {
            program.typeTable[i].name = NULL;
        }
    }

    program.constants = ArenaAlloc(&program.arena, sizeof(Value) * program.constantCount);

    for (u16 i = 0; i < program.constantCount; i++) {
        u8 type;
        fread(&type, sizeof(u8), 1, file);
        program.constants[i].type = type;
        fread(&program.constants[i].rawData, sizeof(i64), 1, file);
    }

    program.functions = ArenaAlloc(&program.arena, sizeof(Function) * program.functionCount);

    for (u16 i = 0; i < program.functionCount; i++) {
        fread(&program.functions[i].localCount, sizeof(u16), 1, file);
        fread(&program.functions[i].paramCount, sizeof(u16), 1, file);
        fread(&program.functions[i].maxRegCount, sizeof(u16), 1, file);

        u32 bytecodeCount;
        fread(&bytecodeCount, sizeof(u32), 1, file);
        program.functions[i].bytecodeCount = bytecodeCount;
        program.functions[i].bytecode = ArenaAlloc(&program.arena, bytecodeCount);
        fread(program.functions[i].bytecode, 1, bytecodeCount, file);
    }

    program.nativeFunctionNames = ArenaAlloc(&program.arena, sizeof(char*) * program.nativeFunctionCount);

    for (u16 i = 0; i < program.nativeFunctionCount; i++) {
        u16 nameLength;
        fread(&nameLength, sizeof(u16), 1, file);
        program.nativeFunctionNames[i] = ArenaAlloc(&program.arena, nameLength + 1);
        fread(program.nativeFunctionNames[i], sizeof(char), nameLength, file);
        program.nativeFunctionNames[i][nameLength] = '\0';
    }


    printf("version: %d\n", version);
    printf("entry: %d\n", program.entryFunctionIndex);
    printf("functions: %d\n", program.functionCount);
    printf("types: %d\n", program.typeTableLength);
    printf("constants: %d\n", program.constantCount);
    printf("native functions %d\n", program.nativeFunctionCount);

    for (u16 i = 0; i < program.nativeFunctionCount; i++) {
        printf("native[%d]: name=%s\n", i, program.nativeFunctionNames[i]);
    }

    for (u16 i = 0; i < program.typeTableLength; i++) {
        printf("type[%d]: kind=%d elementType=%d\n", i, program.typeTable[i].kind, program.typeTable[i].elementTypeId);
    }

    for (u16 i = 0; i < program.constantCount; i++) {
        printf("const[%d]: type=%d value=%" PRId64 "\n", i, program.constants[i].type, program.constants[i].rawData);
    }

    for (u16 i = 0; i < program.functionCount; i++) {
        printf("func[%d]: locals=%d params=%d regs=%d bytecode=%d bytes\n",
               i,
               program.functions[i].localCount,
               program.functions[i].paramCount,
               program.functions[i].maxRegCount,
               program.functions[i].bytecodeCount);
    }


    fclose(file);
    return program;
}

void FreeProgram(Program* program) {
    FreeArena(&program->arena);
}
