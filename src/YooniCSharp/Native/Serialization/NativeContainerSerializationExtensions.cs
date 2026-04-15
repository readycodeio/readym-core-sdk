using LiteNetLib.Utils;
using Yooni.Native.Container;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Serialization;

public static class NetSerializationExtensions
{
    public static void Serialize<TKey, TValue, THash>(this in NativeDictionary<TKey, TValue, THash> dict, NetDataWriter writer)
        where TKey : unmanaged, INetSerializable
        where TValue : unmanaged, INetSerializable
        where THash : struct, IHashFunction<TKey>
    {
        writer.Put(dict.Count);
        foreach (var entry in dict)
        {
            entry.Key.Serialize(writer);
            entry.Value.Serialize(writer);
        }
    }
    
    public static void Deserialize<TKey, TValue, THash>(this ref NativeDictionary<TKey, TValue, THash> dict, NetDataReader reader)
        where TKey : unmanaged, INetSerializable
        where TValue : unmanaged, INetSerializable
        where THash : struct, IHashFunction<TKey>
    {
        var count = reader.GetInt();
        dict.Clear();
        for (var i = 0; i < count; i++)
        {
            TKey key = default;
            key.Deserialize(reader);
            TValue value = default;
            value.Deserialize(reader);
            dict[key] = value;
        }
    }
    
    public static void Serialize<TKey, TStorage>(this in NativeFixed<TKey, TStorage> arr, NetDataWriter writer)
        where TKey : unmanaged, INetSerializable 
        where TStorage : unmanaged, IStorage<TKey>
    {
        writer.Put(arr.Count);
        foreach (var key in arr)
        {
            key.Serialize(writer);
        }
    }
    
    public static void Deserialize<TKey, TStorage>(this ref NativeFixed<TKey, TStorage> arr, NetDataReader reader)
        where TKey : unmanaged, INetSerializable 
        where TStorage : unmanaged, IStorage<TKey>
    {
        var count = reader.GetInt();
        arr.Clear();
        for (var i = 0; i < count; i++)
        {
            TKey key = default;
            key.Deserialize(reader);
            arr.Add(key);
        }
    }
    
    public static void Serialize<T>(this in NativeList<T> lst, NetDataWriter writer)
        where T : unmanaged, INetSerializable
    {
        writer.Put(lst.Count);
        foreach (var item in lst)
        {
            item.Serialize(writer);
        }
    }
    
    public static void Deserialize<T>(this ref NativeList<T> lst, NetDataReader reader)
        where T : unmanaged, INetSerializable
    {
        var count = reader.GetInt();
        lst.Clear();
        for (var i = 0; i < count; i++)
        {
            T item = default;
            item.Deserialize(reader);
            lst.Add(item);
        }
    }
    
    public static void Serialize<T, TStorage>(this in NativeRingBuffer<T, TStorage> ring, NetDataWriter writer)
        where T : unmanaged, INetSerializable
        where TStorage : unmanaged, IStorage<T>
    {
        writer.Put(ring.Count);
        foreach (var item in ring)
        {
            item.Serialize(writer);
        }
    }
    
    public static void Deserialize<T, TStorage>(this ref NativeRingBuffer<T, TStorage> ring, NetDataReader reader)
        where T : unmanaged, INetSerializable
        where TStorage : unmanaged, IStorage<T>
    {
        var count = reader.GetInt();
        ring.Clear();
        for (var i = 0; i < count; i++)
        {
            T item = default;
            item.Deserialize(reader);
            ring.Push(item);
        }
    }
    
    public static void Serialize(this in NativeString64 str, NetDataWriter writer)
    {
        writer.Put(str.Length);
        for (var i = 0; i < str.Length; i++)
        {
            writer.Put(str[i]);
        }
    }

    public static void Deserialize(this ref NativeString64 str, NetDataReader reader)
    {
        var length = reader.GetInt();
        str = new NativeString64(reader.RawData, reader.Position, length);
        reader.SkipBytes(length);
    }
    
    public static void Serialize(this in NativeString256 str, NetDataWriter writer)
    {
        writer.Put(str.Length);
        for (var i = 0; i < str.Length; i++)
        {
            writer.Put(str[i]);
        }
    }
}