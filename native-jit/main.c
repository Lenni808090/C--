#include "Headers/loader.h"
#include "Headers/VM.h"

#include <inttypes.h>
#include <stdio.h>

int main(int argc, char** argv) {
    const char* programPath = argc > 1 ? argv[1] : "output.cmm";
    Program program = LoadProgam(programPath);
    Vm vm;

    VmInit(&vm, &program);

    Value finalVal = VmRun(&vm);

    VmFree(&vm);
    FreeProgram(&program);

    fprintf(stdout, "returned value %" PRId64, finalVal.rawData);
    return 0;
}
