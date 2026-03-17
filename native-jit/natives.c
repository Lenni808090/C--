//
// Created by leona on 14.03.2026.
//

#include "Headers/natives.h"

#include <inttypes.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static Value native_print_int(Value* registers,u16 argCount, u16* argRegs) {
    (void)argCount;
    printf("%" PRId64, registers[argRegs[0]].rawData);
    return (Value){.type = VAL_NULL, .rawData = 0};
}


static Value native_print_bool(Value* registers,u16 argCount, u16* argRegs) {
    (void)argCount;
    printf("%s", registers[argRegs[0]].rawData == 0 ? "false" : "true");
    return (Value){.type = VAL_NULL, .rawData = 0};
}


static Value native_print_char(Value* registers,u16 argCount, u16* argRegs) {
    (void)argCount;
    printf("%c", (char)registers[argRegs[0]].rawData);
    return (Value){.type = VAL_NULL, .rawData = 0};
}


static Value native_print_newline(Value* registers,u16 argCount, u16* argRegs) {
    (void)registers;
    (void)argCount;
    (void)argRegs;
    printf("\n");
    return (Value){.type = VAL_NULL, .rawData = 0};
}

static const NativeEntry nativeRegistry[] = {
    {.name = "print_int", .fn  = native_print_int},
    {.name = "print_bool", .fn = native_print_bool},
    {.name = "print_char", .fn = native_print_char},
    {.name = "print_newline", .fn = native_print_newline},
};
static const u16 nativeFunctionCount = sizeof(nativeRegistry) / sizeof(NativeEntry);

NativeFn*  resolveNativeFunctions(Program* program) {
    NativeFn* resolved = malloc(sizeof(NativeFn) * program->nativeFunctionCount);
    if (resolved == NULL) {
        fprintf(stderr, "unable to alloc resolved natives");
        exit(1);
    }

    for (u16 i = 0; i < program->nativeFunctionCount; i++) {
        bool found = false;
        for (u16 j = 0; j < nativeFunctionCount; j++) {
            if (strcmp(nativeRegistry[j].name, program->nativeFunctionNames[i]) == 0) {
                resolved[i] = nativeRegistry[j].fn;
                found = true;
                break;
            }
        }

        if (found == false) {
            fprintf(stderr, "unresolved native function: %s\n", program->nativeFunctionNames[i]);
            exit(1);
        }
    }

    return resolved;
}
