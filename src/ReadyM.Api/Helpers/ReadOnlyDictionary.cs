using System.Collections;
using System.Collections.Generic;

namespace ReadyM.Api.Helpers;

internal readonly struct ReadOnlyDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary) : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    public int Count
        => dictionary.Count;

    public TValue this[TKey key]
        => dictionary[key];

    public Dictionary<TKey, TValue>.KeyCollection Keys
        => dictionary.Keys;

    public Dictionary<TKey, TValue>.ValueCollection Values
        => dictionary.Values;

    public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
        => dictionary.GetEnumerator();

    public bool ContainsKey(TKey key)
        => dictionary.ContainsKey(key);

    public bool TryGetValue(TKey key, out TValue value)
        => dictionary.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable)dictionary).GetEnumerator();

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        => dictionary.GetEnumerator();

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        => dictionary.Keys;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        => dictionary.Values;
}