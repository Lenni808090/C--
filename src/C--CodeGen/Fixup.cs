namespace CMinus.CodeGen;

readonly struct Fixup {
    public readonly int PatchPos;
    public readonly Label Lable;

    public Fixup(int PatchPos, Label Lable) {
        this.PatchPos = PatchPos;
        this.Lable = Lable;
    }
}
