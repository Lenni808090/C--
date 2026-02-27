using System;

class Program {
    static void Main() {
        TestForwardJump();
        TestWhileLoop();
    }

    static void TestForwardJump() {
        // constants: 111, 222
        var constants = new[]
        {
            new Value(ValueType.Int, 111),
            new Value(ValueType.Int, 222),
        };

        var regs = new Value[8];
        var e = new Emitter();

        // r0 = 111
        e.EmitOp(OpCode.LOAD_CONST);
        e.EmitU8(0); // dst reg
        e.EmitU8(0); // const index

        // jump over "r0 = 222"
        e.Jump("L_end");

        // r0 = 222 (should be skipped)
        e.EmitOp(OpCode.LOAD_CONST);
        e.EmitU8(0);
        e.EmitU8(1);

        e.DefineLabel("L_end");

        // return r0
        e.EmitOp(OpCode.RETURN);
        e.EmitU8(0);

        e.PatchAll();

        var vm = new VM(regs, constants, e.ToArray());
        var result = vm.Run();

        Console.WriteLine($"ForwardJump result: {result.RawData} (expected 111)");
    }

    static void TestWhileLoop() {
        // constants: iStart=0, limit=5, one=1
        var constants = new[]
        {
            new Value(ValueType.Int, 0),
            new Value(ValueType.Int, 5),
            new Value(ValueType.Int, 1),
        };

        var regs = new Value[8];
        var e = new Emitter();

        // r0=i, r1=limit, r2=one, r3=cond
        e.EmitOp(OpCode.LOAD_CONST); e.EmitU8(0); e.EmitU8(0); // r0 = 0
        e.EmitOp(OpCode.LOAD_CONST); e.EmitU8(1); e.EmitU8(1); // r1 = 5
        e.EmitOp(OpCode.LOAD_CONST); e.EmitU8(2); e.EmitU8(2); // r2 = 1

        e.DefineLabel("L_start");

        // r3 = (r0 < r1)
        e.EmitOp(OpCode.CMP_LT_INT);
        e.EmitU8(3); // dst bool
        e.EmitU8(0); // a
        e.EmitU8(1); // b

        // if false -> end
        e.JumpIfFalse(3, "L_end");

        // r0 = r0 + r2
        e.EmitOp(OpCode.ADD_INT);
        e.EmitU8(0); // dst
        e.EmitU8(0); // b
        e.EmitU8(2); // c

        // back to start
        e.Jump("L_start");

        e.DefineLabel("L_end");

        // return r0
        e.EmitOp(OpCode.RETURN);
        e.EmitU8(0);

        e.PatchAll();

        var vm = new VM(regs, constants, e.ToArray());
        var result = vm.Run();

        Console.WriteLine($"WhileLoop result: {result.RawData} (expected 5)");
    }
}