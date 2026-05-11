using System.Collections.Generic;

namespace ReadyM.Api.Multiplayer.Compat;

#if !NETSTANDARD2_1_OR_GREATER

public static class DictionaryExtensions
{
    public static TValue? GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey key)
        where TKey : notnull
    {
        return !self.TryGetValue(key, out var value) ? default : value;
    }
}

#endif