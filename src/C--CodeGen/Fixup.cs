readonly struct Fixup {
    public readonly int PatchPos;
    public readonly string LableName;

    public Fixup(int PatchPos, string LableName) {
        this.PatchPos = PatchPos;
        this.LableName = LableName;
    }
}