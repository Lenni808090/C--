//
// Created by leona on 14.03.2026.
//

#ifndef NATIVES_H
#define NATIVES_H
#include "types.h"



typedef struct {
    const char* name;
    NativeFn fn;
}NativeEntry;

NativeFn*  resolveNativeFunctions(Program* program);
#endif // NATIVES_H
