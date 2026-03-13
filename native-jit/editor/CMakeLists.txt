cmake_minimum_required(VERSION 3.20)
project(cmm_runtime C)

set(CMAKE_EXPORT_COMPILE_COMMANDS ON)
set(CMAKE_C_STANDARD 17)

add_executable(cmm_runtime
        ../main.c
        ../loader.c
        ../VM.c
        ../Headers/VM.h
        ../value.c
        ../Headers/value.h
        ../Heap.c
        ../Headers/Heap.h
)

target_compile_options(cmm_runtime PRIVATE
        -Wall -Wextra -Werror -pedantic
        -g -O0
        -fsanitize=address
        -fno-omit-frame-pointer
        -Wsign-conversion
)

target_link_options(cmm_runtime PRIVATE
        -fsanitize=address
)