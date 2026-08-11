using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ReadyM.Api.Helpers;

internal class BiMap<TForwardKey, TReverseKey>
    where TForwardKey : notnull
    where TReverseKey : notnull
{
    public Indexer<TForwardKey, TReverseKey> Forward { get; }
    public Indexer<TReverseKey, TForwardKey> Reverse { get; }

    const string DuplicateKeyErrorMessage = "Duplicate key found in BiMap";

    public BiMap()
    {
        var forwardMap = new Dictionary<TForwardKey, TReverseKey>();
        var reverseMap = new Dictionary<TReverseKey, TForwardKey>();
        Forward = new Indexer<TForwardKey, TReverseKey>(forwardMap, reverseMap);
        Reverse = new Indexer<TReverseKey, TForwardKey>(reverseMap, forwardMap);
    }

    public BiMap(Dictionary<TForwardKey, TReverseKey> oneWayMap)
    {
        var inverseOneWayMap = oneWayMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        Forward = new Indexer<TForwardKey, TReverseKey>(oneWayMap, inverseOneWayMap);
        Reverse = new Indexer<TReverseKey, TForwardKey>(inverseOneWayMap, oneWayMap);
    }

    public void Add(TForwardKey t1, TReverseKey t2)
    {
        if (Forward.ContainsKey(t1))
            throw new ArgumentException(DuplicateKeyErrorMessage, nameof(t1));
        if (Reverse.ContainsKey(t2))
            throw new ArgumentException(DuplicateKeyErrorMessage, nameof(t2));

        Forward.Add(t1, t2);
    }

    public void ForceAdd(TForwardKey t1, TReverseKey t2)
    {
        Forward.Remove(t1);
        Reverse.Remove(t2);
        Forward.Add(t1, t2);
    }

    public bool Remove(TForwardKey forwardKey, out TReverseKey? reverseKey)
        => Forward.Remove(forwardKey, out reverseKey);

    public int Count()
        => Forward.Count();

    public void Clear()
    {
        Forward.Clear();
        Reverse.Clear();
    }

    public Dictionary<TForwardKey, TReverseKey>.Enumerator GetEnumerator()
        => Forward.GetEnumerator();

    /// <summary>
    /// Publically read-only lookup to prevent inconsistent state between forward and reverse map lookups
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class Indexer<TKey, TValue>
        where TKey : notnull
        where TValue : notnull
    {
        private readonly Dictionary<TKey, TValue> _dictionary;
        private readonly Dictionary<TValue, TKey> _inverse;

        public Indexer(Dictionary<TKey, TValue> dictionary, Dictionary<TValue, TKey> inverse)
        {
            _dictionary = dictionary;
            _inverse = inverse;
        }

        public TValue this[TKey index]
        {
            get => _dictionary[index];
            set
            {
                if (ContainsKey(index))
                {
                    Remove(index);
                }

                Add(index, value);
            }
        }

        public static implicit operator Dictionary<TKey, TValue>(Indexer<TKey, TValue> indexer)
            => new(indexer._dictionary);

        internal void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
            _inverse.Add(value, key);
        }

        internal bool Remove(TKey key)
        {
            var result = _dictionary.Remove(key, out var value);
            if (result)
            {
                Debug.Assert(value != null);
                _inverse.Remove(value);
            }

            return result;
        }

        internal bool Remove(TKey key, out TValue? value)
        {
            var result = _dictionary.Remove(key, out value);
            if (result)
            {
                Debug.Assert(value != null);
                _inverse.Remove(value);
            }

            return result;
        }

        internal int Count()
            => _dictionary.Count;

        public bool ContainsKey(TKey key)
            => _dictionary.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value)
            => _dictionary.TryGetValue(key, out value);

        public void Clear()
        {
            _dictionary.Clear();
            _inverse.Clear();
        }

        public IEnumerable<TKey> Keys
            => _dictionary.Keys;

        public IEnumerable<TValue> Values
            => _dictionary.Values;

        /// <summary>
        /// Deep copy lookup as a dictionary
        /// </summary>
        /// <returns></returns>
        public Dictionary<TKey, TValue> ToDictionary()
            => new(_dictionary);

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
            => _dictionary.GetEnumerator();
    }
}