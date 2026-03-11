#include "loader.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

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

    fclose(file);
    return program;
}