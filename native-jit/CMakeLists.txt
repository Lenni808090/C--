add_executable(cmm_runtime
    arena.c
    Gc.c
    Heap.c
    loader.c
    main.c
    natives.c
    value.c
    VM.c
)

target_compile_features(cmm_runtime PRIVATE c_std_11)

target_include_directories(cmm_runtime PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/Headers
)

if(MSVC)
    target_compile_options(cmm_runtime PRIVATE /W4)
else()
    target_compile_options(cmm_runtime PRIVATE -Wall -Wextra -Wpedantic)
endif()

set(_cmm_sample_program "${CMAKE_SOURCE_DIR}/output.cmm")
if(EXISTS "${_cmm_sample_program}")
    add_custom_command(TARGET cmm_runtime POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${_cmm_sample_program}"
            "$<TARGET_FILE_DIR:cmm_runtime>/output.cmm"
    )
endif()
