#include "loader.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include <inttypes.h>

static const u8 MAGIC_NUMBER[] = {0x43, 0x4D, 0x4D, 0x00};

Program LoadProgam(const char* path) {
    FILE* file = fopen(path, "rb");
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

    u16 version;
    fread(&version, sizeof(u16), 1, file);

    Program program;
    fread(&program.entryFunctionInd, sizeof(u16), 1, file);
    fread(&program.functionCount, sizeof(u16), 1, file);
    fread(&program.typeTableLength, sizeof(u16), 1, file);
    fread(&program.constantCount, sizeof(u16), 1, file);

    printf("version: %d\n", version);
    printf("entry: %d\n", program.entryFunctionInd);
    printf("functions: %d\n", program.functionCount);
    printf("types: %d\n", program.typeTableLength);
    printf("constants: %d\n", program.constantCount);

    program.typeTable = (RuntimeTypeDesc*)malloc(sizeof(RuntimeTypeDesc) * program.typeTableLength);
    if (program.typeTable == NULL) {
        fprintf(stderr, "failed to alloc type table");
        exit(1);
    }

    for (u16 i = 0; i < program.typeTableLength; i++) {
        u8 kind;
        fread(&kind, sizeof(u8), 1, file);
        program.typeTable[i].kind = kind;

        fread(&program.typeTable[i].elementTypeId, sizeof(u16), 1, file);

        u16 nameLength;
        fread(&nameLength, sizeof(u16), 1, file);
        program.typeTable[i].nameLength = nameLength;

        if (nameLength > 0) {
            program.typeTable[i].name = (char*)malloc(sizeof(char) * (nameLength + 1));
            fread(program.typeTable[i].name, sizeof(char), nameLength, file);
            program.typeTable[i].name[nameLength] = '\0';
        } else {
            program.typeTable[i].name = NULL;
        }
    }

    program.constants = (Value*)malloc(sizeof(Value) * program.constantCount);
    if (program.constants == NULL) {
        fprintf(stderr, "failed to alloc constants");
        exit(1);
    }

    for (u16 i = 0; i < program.constantCount; i++) {
        u8 type;
        fread(&type, sizeof(u8), 1, file);
        program.constants[i].type = type;
        fread(&program.constants[i].rawData, sizeof(i64), 1, file);
    }

    program.functions = (Function*)malloc(sizeof(Function) * program.functionCount);
    if (program.functions == NULL) {
        fprintf(stderr, "failed to alloc functions");
        exit(1);
    }

    for (u16 i = 0; i < program.functionCount; i++) {
        fread(&program.functions[i].localCount, sizeof(u16), 1, file);
        fread(&program.functions[i].paramCount, sizeof(u16), 1, file);
        fread(&program.functions[i].maxRegCount, sizeof(u16), 1, file);

        u32 bytecodeCount;
        fread(&bytecodeCount, sizeof(u32), 1, file);
        program.functions[i].bytecodeCount = bytecodeCount;
        program.functions[i].bytecode = (u8*)malloc(bytecodeCount);
        if (program.functions[i].bytecode == NULL) {
            fprintf(stderr, "failed to alloc bytecode for function %d", i);
            exit(1);
        }
        fread(program.functions[i].bytecode, 1, bytecodeCount, file);
    }

    printf("version: %d\n", version);
    printf("entry: %d\n", program.entryFunctionInd);
    printf("functions: %d\n", program.functionCount);
    printf("types: %d\n", program.typeTableLength);
    printf("constants: %d\n", program.constantCount);

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