namespace CMinus.Runtime;

class Heap {
    public List<HeapObject> heapObjects;

    public Heap() {
        heapObjects = new();
    }


    public int Allocate(HeapObject heapObject) {
        int id = heapObjects.Count;
        heapObject.id = id;
        heapObjects.Add(heapObject);
        return id;
    }

    public HeapObject GetHeapObject(int id) {
        return heapObjects[id];
    }


}



