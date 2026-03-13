//
// Created by leona on 11.03.2026.
//

#include "Headers/value.h"

#include <stdio.h>
#include <stdlib.h>

bool AsBool(Value value) {
    if (value.type != VAL_BOOL) {
        fprintf(stderr, "run time expected a bool value");
        exit(1);
    }
    return value.rawData == 0 ? false : true;
}
i32 AsInt(Value value) {
    if (value.type != VAL_INT) {
        fprintf(stderr, "run time expected a int value");
        exit(1);
    }
    return (i32)value.rawData;
}

u32 AsHeapReference(Value value) {
    if (value.type != VAL_HEAPREF) {
        fprintf(stderr, "run time expected a heapref");
        exit(1);
    }
    return (u32)value.rawData;
}

