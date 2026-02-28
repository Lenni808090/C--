enum OpCode : byte {
    LOAD_CONST,

    LOAD_LOCAL,
    STORE_LOCAL,

    RETURN,

    ADD_INT,
    SUBTRACT_INT,
    MULTIPLY_INT,
    DIVIDE_INT,

    JUMP,
    JUMP_IF_FALSE,
    JUMP_IF_TRUE,


    CMP_EQ_INT,
    CMP_LT_INT,
    CMP_MT_INT,

}