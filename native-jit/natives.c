//
// Created by leona on 14.03.2026.
//

#include "Headers/natives.h"

#include <inttypes.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static Value CreateNullValue(void) {
    Value value;
    value.type = VAL_NULL;
    value.rawData = 0;
    return value;
}

static Value native_print_int(Value* registers,u16 argCount, u16* argRegs) {
    (void)argCount;
    printf("%" PRId64, registers[argRegs[0]].rawData);
    return CreateNullValue();
}


static Value native_print_bool(Value* registers,u16 argCount, u16* argRegs) {
    (void)argCount;
    printf("%s", registers[argRegs[0]].rawData == 0 ? "false" : "true");
    return CreateNullValue();
}


static Value native_print_char(Value* registers,u16 argCount, u16* argRegs) {
    (void)argCount;
    printf("%c", (char)registers[argRegs[0]].rawData);
    return CreateNullValue();
}


static Value native_print_newline(Value* registers,u16 argCount, u16* argRegs) {
    (void)registers;
    (void)argCount;
    (void)argRegs;
    printf("\n");
    return CreateNullValue();
}

static const NativeEntry nativeRegistry[] = {
    {"print_int", native_print_int},
    {"print_bool", native_print_bool},
    {"print_char", native_print_char},
    {"print_newline", native_print_newline},
};
static const u16 nativeFunctionCount = (u16)(sizeof(nativeRegistry) / sizeof(nativeRegistry[0]));

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
