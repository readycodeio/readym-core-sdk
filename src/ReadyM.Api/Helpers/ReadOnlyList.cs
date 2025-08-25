using System.Collections;
using System.Collections.Generic;

namespace ReadyM.Api.Helpers;

public readonly struct ReadOnlyList<T>(List<T> list) : IReadOnlyList<T>
{
    public int Count
        => list.Count;

    public T this[int index]
        => list[index];

    public List<T>.Enumerator GetEnumerator()
        => list.GetEnumerator();
    
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable)list).GetEnumerator();
    
    public bool Contains(T item)
        => list.Contains(item);
    
    private static readonly List<T> _emptyList = new List<T>();
    
    public static ReadOnlyList<T> Empty
        => new ReadOnlyList<T>(_emptyList);

    public ReadOnlyList<T> Copy()
        => new ReadOnlyList<T>(new List<T>(list));
}