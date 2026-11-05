using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ReadyM.Api.Multiplayer.Interop;

/// <exclude/>
public struct SizeOfHelper<T>
    where T : unmanaged
{
    public static readonly int Size = Unsafe.SizeOf<T>();
}

/// <exclude/>
public struct SizeOfHelper
{
    [ThreadStatic]
    private static Dictionary<Type, int?>? _cache;
    
    public static int? SizeOfType(Type type)
    {
        _cache ??= new();
        
        if (!_cache.TryGetValue(type, out var size))
        {
            try
            {
                var sizeOfHelperType = typeof(SizeOfHelper<>).MakeGenericType(type);
                var sizeField = sizeOfHelperType.GetField("Size",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                size = (int)(sizeField?.GetValue(null) ?? throw new InvalidOperationException());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                size = null;
            }
            
            _cache.Add(type, size);
        }
        
        return size;
    }
}