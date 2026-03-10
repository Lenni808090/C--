using System.Runtime.InteropServices;
using System.Text;
using CMinus.CodeGen;
using CMinus.Runtime;
namespace CMinus.Serialization;

class ByteWriter {
    byte[] magicNumber = new byte[] { 0x43, 0x4D, 0x4D, 0x00 };

    CompiledProgram compiledProgram;
    FileStream fileStream;
    BinaryWriter writer;
    ushort currentVersion;
    public ByteWriter(CompiledProgram compiledProgram, ushort currentVersion) {
        this.currentVersion = currentVersion;
        this.compiledProgram = compiledProgram;
        fileStream = new("output.cmm", FileMode.Create);
        writer = new(fileStream);
    }

    public void bytefyCompiledUnit() {
        // metadata
        writer.Write(magicNumber);
        writer.Write(currentVersion);
        writer.Write((ushort)compiledProgram.entryFuncInd);
        writer.Write((ushort)compiledProgram.compiledFunctions.Length);
        writer.Write((ushort)compiledProgram.typeTable.Length);
        writer.Write((ushort)compiledProgram.constants.Length);


        //type table
        foreach (RuntimeTypeDesc runtimeType in compiledProgram.typeTable) {
            writer.Write((byte)runtimeType.Kind);

            if (runtimeType.ElementTypeId is null) {
                writer.Write((ushort)0xFFFF);
            }
            else {
                writer.Write((ushort)runtimeType.ElementTypeId);
            }

            if (runtimeType.Name is null) {
                writer.Write((ushort)0);
            }
            else {
                byte[] nameBytes = Encoding.UTF8.GetBytes(runtimeType.Name);
                writer.Write((ushort)nameBytes.Length);
                writer.Write(nameBytes);
            }
        }

        //constants pool
        foreach (Value consti in compiledProgram.constants) {
            writer.Write((byte)consti.Type);
            writer.Write((long)consti.RawData);
        }

        //functions:
        foreach (CompiledFunction function in compiledProgram.compiledFunctions) {
            writer.Write((ushort)function.localCount);
            writer.Write((ushort)function.paramCount);
            writer.Write((ushort)function.maxRegCount);
            writer.Write((int)function.bytecode.Length);
            writer.Write(function.bytecode);
        }
    }
}