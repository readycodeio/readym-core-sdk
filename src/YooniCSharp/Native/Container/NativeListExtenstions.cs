using System;
using System.Collections.Generic;

namespace Yooni.Native.Container;

public static class NativeListExtensions
{
    public static bool Remove<T>(ref this NativeList<T> list, T value)
        where T : unmanaged
        => list.Remove(value, EqualityComparer<T>.Default);

    public static bool Remove<T, TComparer>(ref this NativeList<T> list, T value, TComparer comparer)
        where T : unmanaged
        where TComparer : IEqualityComparer<T>
    {
        list.MarkChange();

        var index = -1;
        for (var i = 0; i < list.Count; ++i)
        {
            var x = list[i];
            if (comparer.Equals(value, x))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
            return false;

        list.RemoveAt(index);
        return true;
    }

    public static void Trim<T>(ref this NativeList<T> values, int count, int startIndex = 0)
        where T : unmanaged
    {
        values.MarkChange();

        if (startIndex < 0)
            throw new ArgumentException();
        if (values.Count < startIndex + count)
            throw new InvalidOperationException();

        if (startIndex > 0)
        {
            for (var i = 0; i < count; i++)
            {
                values[i] = values[i + startIndex];
            }
        }

        values.RemoveRange(count, values.Count - count);
    }

    public static void Trim<T>(ref this NativeList<T> values, in NativeList<T> source, int count, int startIndex = 0)
        where T : unmanaged
    {
        values.MarkChange();

        if (startIndex < 0)
            throw new ArgumentException();
        if (source.Count < startIndex + count)
            throw new InvalidOperationException();

        values.Clear();
        for (var i = 0; i < count; i++)
        {
            values.Add(source[i + startIndex]);
        }
    }
}
