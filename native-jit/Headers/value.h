//
// Created by leona on 11.03.2026.
//

#ifndef VALUE_H
#define VALUE_H
#include <stdbool.h>
#include "types.h"
#include "Heap.h"


bool AsBool(Value value);
i32 AsInt(Value value);
ObjHeader* AsHeapPointer(Value value);
#endif // VALUE_H
