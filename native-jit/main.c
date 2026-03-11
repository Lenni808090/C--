#include "Headers/loader.h"
#include "Headers/VM.h"

#include <inttypes.h>
#include <stdio.h>

int main(void) {
    Program program = LoadProgam("/mnt/c/Users/leona/source/repos/C--/output.cmm");
    Vm vm;

    VmInit(&vm, &program);

    Value finalVal = VmRun(&vm);

    VmFree(&vm);
    FreeProgram(&program);

    fprintf(stdout, "returned value %" PRId64 ,  finalVal.rawData);
    return 0;
}