using System.Collections.Generic;

namespace ReadyM.Api.Helpers;

public static class ReadOnlyListExtensions
{
    public static ReadOnlyList<T> WrapReadOnly<T>(this List<T> list)
        => new ReadOnlyList<T>(list);

    public static ReadOnlyList<T> NullableWrapReadOnly<T>(this List<T>? list)
        => list != null ? new ReadOnlyList<T>(list) : ReadOnlyList<T>.Empty;
}