using System;
using System.Collections;
using System.Collections.Generic;

namespace ReadyM.Api.Helpers;

internal readonly struct ReadOnlyArray<T>(T[] array) : IReadOnlyList<T>
{
    public int Count
        => array.Length;

    public T this[int index]
        => array[index];

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => ((IEnumerable<T>)array).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => array.GetEnumerator();

    public bool Contains(T item)
        => Array.IndexOf(array, item) >= 0;

    public static readonly T[] EmptyArray = [];

    public static ReadOnlyArray<T> Empty => new(EmptyArray);

    public ReadOnlyArray<T> Copy() => new([..array]);
}
