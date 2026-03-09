namespace CMinus.Runtime;

abstract class HeapObject {
    public int id;
    public abstract HeapObjectKind heapObjectKind {
        get;
    }
}

class ArrayObject : HeapObject {
    public override HeapObjectKind heapObjectKind => HeapObjectKind.ArrayObject;

    public int TypeId;
    public int ElementTypeId;
    public Value[] Elements;
    public int Length;
    public ArrayObject(int typeId, int elementTypeId, Value[] Elements) {
        Length = Elements.Length;
        TypeId = typeId;
        ElementTypeId = elementTypeId;
        this.Elements = Elements;
    }
}

enum HeapObjectKind {
    ArrayObject
}

