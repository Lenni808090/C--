struct Label {

    public int FirstInstructionPos;
    public string LabelName;
    public Label(int FirstInstructionPos, string LabelName) {
        this.FirstInstructionPos = FirstInstructionPos;
        this.LabelName = LabelName;
    }
}