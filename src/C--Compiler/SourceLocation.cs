namespace CMinus.Compiler;

public readonly struct SourceLocation {
    public static readonly SourceLocation None = new(0, 0, 0, 0);

    public int Line { get; }
    public int Column { get; }
    public int Start { get; }
    public int Length { get; }

    public bool IsValid => Line > 0;

    public SourceLocation(int line, int column, int start, int length) {
        Line = line;
        Column = column;
        Start = start;
        Length = length;
    }

    public override string ToString() {
        if (!IsValid) {
            return "unknown";
        }

        return $"line {Line}, col {Column}";
    }
}
