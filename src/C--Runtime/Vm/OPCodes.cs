namespace CMinus.Runtime;

enum OpCode : byte {
    LOAD_CONST,

    LOAD_LOCAL,
    STORE_LOCAL,

    RETURN,

    ADD_INT,
    SUBTRACT_INT,
    MULTIPLY_INT,
    DIVIDE_INT,
    NEG_INT,

    NOT,

    JUMP,
    JUMP_IF_FALSE,
    JUMP_IF_TRUE,


    CMP_EQ,
    CMP_LT_INT,
    CMP_LTE_INT,
    CMP_MT_INT,
    CMP_MTE_INT,
    CMP_NEQ,
    MODULUS_INT,


    NEW_ARRAY,
    STORE_ELEMENT,
    LOAD_ELEMNT,
    ARRAY_LENGTH,

    MOVE,

    CALL,

}