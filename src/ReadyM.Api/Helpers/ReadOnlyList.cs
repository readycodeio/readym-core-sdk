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
}