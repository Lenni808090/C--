cmake_minimum_required(VERSION 3.20)

project(CMinusRuntime LANGUAGES C)

option(CMM_ENABLE_SANITIZERS "Build native runtime with sanitizer instrumentation" OFF)

add_executable(cmm_runtime
        arena.c
        Gc.c
        Heap.c
        loader.c
        main.c
        natives.c
        value.c
        VM.c
        JitMemory.c
)


target_compile_features(cmm_runtime PRIVATE c_std_11)

target_include_directories(cmm_runtime PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}
        ${CMAKE_CURRENT_SOURCE_DIR}/Headers
)

if (MSVC)
    target_compile_options(cmm_runtime PRIVATE /W4)
else ()
    target_compile_options(cmm_runtime PRIVATE -Wall -Wextra -Wpedantic)
endif ()

if (CMM_ENABLE_SANITIZERS)
    if (CMAKE_C_COMPILER_ID MATCHES "Clang|GNU")
        target_compile_options(cmm_runtime PRIVATE -fsanitize=address,undefined -fno-omit-frame-pointer)
        target_link_options(cmm_runtime PRIVATE -fsanitize=address,undefined)
    elseif (MSVC)
        target_compile_options(cmm_runtime PRIVATE /fsanitize=address)
        target_link_options(cmm_runtime PRIVATE /fsanitize=address)
    else ()
        message(FATAL_ERROR "Sanitizers are not configured for compiler ${CMAKE_C_COMPILER_ID}")
    endif ()
endif ()

if (CMAKE_SOURCE_DIR STREQUAL CMAKE_CURRENT_SOURCE_DIR)
    set(_cmm_sample_program "${CMAKE_CURRENT_SOURCE_DIR}/../output.cmm")
else ()
    set(_cmm_sample_program "${CMAKE_SOURCE_DIR}/output.cmm")
endif ()

if (EXISTS "${_cmm_sample_program}")
    add_custom_command(TARGET cmm_runtime POST_BUILD
            COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${_cmm_sample_program}"
            "$<TARGET_FILE_DIR:cmm_runtime>/output.cmm"
    )
endif ()
