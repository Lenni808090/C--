namespace CMinus.Runtime;

abstract class HeapObject {
    public int id;
    public abstract HeapObjectKind heapObjectKind {
        get;
    }
}

class ArrayObject : HeapObject {
    public override HeapObjectKind heapObjectKind => HeapObjectKind.ArrayObject;

    public ValueType ElementType;
    public Value[] Elements;
    public int Length;
    public ArrayObject(ValueType ElementType, Value[] Elements) {
        Length = Elements.Length;
        this.ElementType = ElementType;
        this.Elements = Elements;
    }
}

enum HeapObjectKind {
    ArrayObject
}

