#ifndef LOADER_H
#define LOADER_H

#include "types.h"

Program LoadProgam(const char* path);

void FreeProgram(Program* program);

#endif
