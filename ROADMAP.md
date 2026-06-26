# C-- Native Runtime Roadmap

## Architecture Overview

C-- currently has two halves:

- **Compiler (C#):** source -> lexer -> parser -> binder -> IR builder -> control-flow analysis -> stack-map analysis -> code generator -> binary serializer -> `.cmm`
- **Runtime (C):** `.cmm` loader -> interpreter -> native functions -> heap -> garbage collector

The `.cmm` binary format is the bridge between both halves. The compiler writes it, the native runtime reads it.

Primary target platform is **Windows x64**. The runtime and baseline JIT should be designed Windows-first, using the Microsoft x64 calling convention and Win32 APIs such as `VirtualAlloc` and `VirtualFree` for executable memory. Linux/macOS support is not a current goal; it can be added later behind platform guards if it becomes useful.

## Current Status

- [x] Phase 1.1: Binary format design
- [x] Phase 1.2: C# serializer
- [x] Phase 1.3: C loader
- [x] Phase 1.4: VM execution loop
- [x] Phase 1.5: Heap and array support
- [x] Phase 1.6: Interpreter validation
- [x] Phase 2.1: Memory arena allocator
- [ ] Phase 2.2: Structured error handling
- [x] Phase 2.3: Native function interface
- [x] Phase 2.4: Stack maps
- [x] Phase 3.1: GC-aware heap redesign
- [x] Phase 3.2: Root enumeration
- [x] Phase 3.3: Mark phase
- [x] Phase 3.4: Sweep phase with free list
- [x] Phase 3.5: GC triggering on allocation failure
- [x] Phase 3.6: GC testing with allocation stress and live-data preservation
- [x] Phase 4.1: Executable memory allocation
- [ ] Phase 4.2: Assembler / code emitter
- [ ] Phase 4.3: Value representation for JIT frames
- [ ] Phase 4.4: Template compilation
- [ ] Phase 4.5: Runtime helper calls from JIT code
- [ ] Phase 4.6: Function prologue and epilogue
- [ ] Phase 4.7: Patching and linking
- [ ] Phase 4.8: Interpreter-vs-JIT validation

**You are here:** Phase 4.2.

## Phase 1: Interpreter

**Goal:** A working C runtime that produces the same results as the C# compiler/runtime reference path.

Completed work:

- Binary `.cmm` format with header, type table, constant pool, function table, native import table, and stack maps
- C# serializer in `src/C--Serialization/ByteWriter.cs`
- C loader in `native-jit/loader.c`
- Interpreter dispatch loop in `native-jit/VM.c`
- Heap-allocated arrays, bounds checks, null checks, and array length support
- Native function resolution and calls

## Phase 2: Runtime Infrastructure

**Goal:** Support systems shared by the interpreter and the future JIT.

Completed work:

- Arena allocator in `native-jit/arena.c`
- Name-based native function interface in `native-jit/natives.c`
- Runtime function table for native and user functions
- Stack-map analysis in C# and stack-map loading in C

Deferred work:

- Structured runtime error handling. Current runtime checks are useful, but mostly report through `fprintf` and terminate. Better error codes, bytecode offsets, and source mapping can wait until the JIT path is established.

## Phase 3: Garbage Collector

**Goal:** Replace grow-forever heap behavior with automatic memory management.

Completed work:

- Contiguous managed heap with bump allocation
- Uniform object headers
- Direct pointer heap references
- Mark-and-sweep collection
- Stack-map-driven root enumeration
- Recursive tracing for reference arrays
- Free-list reclamation and reuse
- Allocation retry after GC

## Phase 4: Baseline JIT Compiler

**Goal:** Compile bytecode to native x86-64 code, initially one bytecode instruction at a time.

The first JIT should be deliberately simple. Keep VM registers as frame memory slots, load values into CPU registers for each operation, then store results back. Complex operations such as allocation, array access, bounds checking, and GC safepoints should call C runtime helper functions at first.

### 4.1 Executable Memory Allocation

Implemented a small JIT memory module:

- `native-jit/Headers/JitMemory.h`
- `native-jit/JitMemory.c`
- Allocate executable pages with `VirtualAlloc`
- Free pages with `VirtualFree`
- Track capacity and current write position
- Provide an append API for raw bytes
- Add the files to `native-jit/CMakeLists.txt`

Suggested API:

```c
typedef struct {
    u8* start;
    u8* current;
    u8* end;
} JitCodeBuffer;

void InitJitCodeBuffer(JitCodeBuffer* buffer, u32 capacity);
void FreeJitCodeBuffer(JitCodeBuffer* buffer);
void EmitByte(JitCodeBuffer* buffer, u8 byte);
void EmitBytes(JitCodeBuffer* buffer, const u8* bytes, u32 count);
u32 GetJitCodeOffset(const JitCodeBuffer* buffer);
```

Validated by:

- Allocate a small code buffer
- Emit bytes for a trivial function: `mov rax, 42; ret`
- Cast the buffer start to a function pointer
- Call it and verify it returns `42`

### 4.2 Assembler / Code Emitter

Add a small x86-64 emitter layer above `JitMemory`.

New files:

- `native-jit/Headers/X64Emitter.h`
- `native-jit/X64Emitter.c`

Responsibilities:

- Keep raw x64 opcode bytes isolated in one architecture-specific module.
- Provide named instruction helpers so future JIT code does not write magic bytes directly.
- Represent semantic concepts such as registers, conditions, and operand sizes with small enums or constants.
- Continue writing into `JitMemBuffer`; this module should not allocate executable memory itself.
- Avoid any dependency on C-- bytecode, `Vm`, `Value`, heap objects, GC, or stack maps.

Do not build a bytecode compiler in this phase. The emitter only knows x64 instruction encoding. The future baseline JIT will call into this module later.

Initial API direction:

- `emit_mov_rax_imm64`
- `emit_mov_reg_imm64`
- `emit_mov_reg_reg`
- `emit_add_reg_reg`
- `emit_sub_reg_reg`
- `emit_cmp_reg_reg`
- `emit_jmp_rel32`
- `emit_call_rel32` or absolute-call trampoline support
- `emit_ret`

Start deliberately smaller than this full list:

- First replace the manual `mov rax, 42; ret` byte array with emitter calls.
- Then generalize from `rax`-only to register-aware helpers.
- Then add simple arithmetic instructions.
- Only after arithmetic works should jumps and calls be added.

Design guidance:

- Good abstraction: `EmitMovRegImm64(buffer, X64_RAX, 42)`.
- Bad abstraction: a giant enum where every raw byte has a name.
- Good constants: names for meaningful opcode concepts such as REX.W, RET, and MOV-r64-imm64 base opcode.
- Good enums: x64 registers and condition codes.
- Raw bytes are allowed inside `X64Emitter.c`; they should not spread into VM or future JIT lowering code.

Validation target:

- Recreate the Phase 4.1 test using only emitter functions: emit a function that returns `42`.
- Add at least one arithmetic smoke test, such as loading two values into registers, adding them, returning the result.
- Run the normal debug preset first. Sanitizers are useful for the runtime, but generated executable code should initially be validated without sanitizer instrumentation.

### 4.3 Value Representation for JIT Frames

Start with the conservative model:

- VM registers remain memory slots in a JIT frame
- JIT code loads operands into scratch x86 registers
- JIT code stores results back into frame slots

This avoids full register allocation in the baseline JIT.

### 4.4 Template Compilation

Translate simple opcodes first:

- `LOAD_CONST`
- `MOVE`
- `ADD_INT`
- `SUB_INT`
- `MULT_INT`
- `NEG_INT`
- `RETURN`

Then add control flow:

- `JUMP`
- `JUMP_IF_FALSE`
- `JUMP_IF_TRUE`
- comparisons

Then add calls and heap operations through runtime helpers.

### 4.5 Runtime Helper Functions

Keep complex operations in C while the JIT is young:

- array allocation
- array load/store
- bounds checks
- null checks
- native calls
- GC safepoints

### 4.6 Function Entry and Exit

Implement a Windows x64 compliant prologue and epilogue:

- Preserve required callee-saved registers
- Maintain 16-byte stack alignment before calls
- Reserve 32 bytes of shadow space at call sites
- Return the final `Value` according to the chosen ABI contract

### 4.7 Patching and Linking

Add fixups for:

- intra-function jumps
- calls to already-compiled functions
- lazy compilation of called functions
- runtime helper call addresses

### 4.8 Validation

Every supported test program should run through both:

- interpreter mode
- JIT mode

Outputs must match exactly.

## Phase 5: Optimizing JIT

Only start this after the baseline JIT is correct.

Possible optimization work:

- Linear-scan register allocation
- Constant folding
- Constant propagation
- Dead-code elimination
- Function inlining
- Loop optimizations
- Bounds-check elimination for simple loops

## Phase 6: Language Features

These can be interleaved with runtime work, but each affects compiler, bytecode, runtime, and GC:

- Strings
- Object or struct types
- Methods
- Virtual dispatch
- Closures
- First-class functions
- Structured error handling

## Next Step

Start with **Phase 4.2: assembler / code emitter**. Create the x64 emitter files and move the working `mov rax, 42; ret` test away from manual byte arrays and into named emitter functions. Do not connect this to bytecode yet.
