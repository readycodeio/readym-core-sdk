using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ReadyM.Api.Multiplayer.Interop;

internal static class BlittableHelper
{
    private struct CacheEntry
    {
        public bool Result;
        public List<string> Errors;
    }
    
    [ThreadStatic]
    private static Dictionary<Type, CacheEntry>? _cache;
    
    public static bool IsBlittable(Type type, List<string>? outErrors = null)
    {
        _cache ??= new();
        
        if (!_cache.TryGetValue(type, out var entry))
        {
            entry.Errors = [];
            entry.Result = IsBlittable(type, new HashSet<Type>(), type.Name, entry.Errors);
            _cache.Add(type, entry);
        }
        
        outErrors?.AddRange(entry.Errors);
        return entry.Result;
    }

    private static bool IsBlittable(Type type, HashSet<Type> seen, string path, List<string> outErrors)
    {
        if (!seen.Add(type))
            return true;

        if (type.IsPointer)
            return true;

        if (!type.IsValueType)
        {
            outErrors.Add($"{path} is not a value type");
            return false;
        }

        if (type.IsEnum)
            return IsBlittable(Enum.GetUnderlyingType(type), seen, path, outErrors);
        
        if (type == typeof(byte)   || type == typeof(sbyte) ||
            type == typeof(short)  || type == typeof(ushort) ||
            type == typeof(int)    || type == typeof(uint) ||
            type == typeof(long)   || type == typeof(ulong) ||
            type == typeof(IntPtr) || type == typeof(UIntPtr) ||
            type == typeof(float)  || type == typeof(double))
            return true;

        if (type == typeof(bool))
        {
            outErrors.Add($"{path} is `bool` (not blittable)");
            return false;
        }

        if (type == typeof(char))
        {
            outErrors.Add($"{path} is `char` (not blittable)");
            return false;
        }

        if (type == typeof(decimal))
        {
            outErrors.Add($"{path} is `decimal` (not blittable)");
            return false;
        }

        var layout = type.StructLayoutAttribute;
        if (layout == null)
        {
            outErrors.Add($"{path} is a struct with no layout specified");
            return false;
        }

        if (layout.Value != LayoutKind.Sequential && layout.Value != LayoutKind.Explicit)
        {
            outErrors.Add($"{path} is a struct has the wrong layout kind ({layout.Value})");
            return false;
        }

        var fields = type.GetFields(BindingFlags.Instance |
                                    BindingFlags.Public |
                                    BindingFlags.NonPublic);

        return fields.All(f => IsBlittable(f.FieldType, seen, $"{path}.{f.Name}", outErrors));
    }
}