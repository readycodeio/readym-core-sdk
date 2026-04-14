using System;
using System.Collections.Generic;
using System.Reflection;

namespace Yooni.Native.Container;

public static class NativeHelper
{
    private static readonly Dictionary<Type, bool> Cache =
        new Dictionary<Type, bool>();

    public static bool IsUnmanaged(this Type t)
    {
        if (Cache.TryGetValue(t, out var result))
            return result;

        if (t.IsPrimitive || t.IsPointer || t.IsEnum)
            result = true;
        else if (!t.IsValueType)
            result = false;
        else
        {
            var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            result = true;
            foreach (var f in fields)
            {
                if (!f.FieldType.IsUnmanaged())
                {
                    result = false;
                    break;
                }
            }
        }

        Cache.Add(t, result);
        return result;
    }
}