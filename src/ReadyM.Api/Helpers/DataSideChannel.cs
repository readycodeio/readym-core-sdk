using System;
using System.Collections.Generic;

namespace ReadyM.Api.Helpers;

public class DataSideChannel
{
    private class EntryBase
    {
        public bool IsSet;
    }

    private class Entry<T> : EntryBase
    {
        public T? Data;
    }
    
    private readonly Dictionary<Type, EntryBase> _entries = new();

    public readonly struct Scope<T> : IDisposable
    {
        private readonly DataSideChannel _channel;

        public Scope(DataSideChannel channel, T data)
        {
            _channel = channel;
            _channel.PushData(data);
        }

        public void Dispose()
        {
            _channel.PopData<T>();
        }
    }
    
    public Scope<T> PushScope<T>(T data = default)
        where T : struct
        => new(this, data);

    public void PushData<T>(T data)
    {
        if (!_entries.TryGetValue(typeof(T), out var entry))
        {
            entry = new Entry<T> { Data = data };
            _entries.Add(typeof(T), entry);
        }
        
        if (entry.IsSet)
            throw new InvalidOperationException($"Data of type {typeof(T)} is already set in the side channel.");
        
        entry.IsSet = true;
        var typedEntry = (Entry<T>)entry;
        typedEntry.Data = data;
    }
    
    public void PopData<T>()
    {
        if (!_entries.TryGetValue(typeof(T), out var entry) || !entry.IsSet)
            throw new InvalidOperationException($"Data of type {typeof(T)} is not set in the side channel.");
        
        entry.IsSet = false;
    }
    
    public T? GetData<T>()
    {
        if (!_entries.TryGetValue(typeof(T), out var entry) || !entry.IsSet)
            throw new InvalidOperationException($"Data of type {typeof(T)} is not set in the side channel.");
        
        var typedEntry = (Entry<T>)entry;
        return typedEntry.Data;
    }
    
    public bool TryGetData<T>(out T? data)
    {
        if (!_entries.TryGetValue(typeof(T), out var entry) || !entry.IsSet)
        {
            data = default;
            return false;
        }
        
        var typedEntry = (Entry<T>)entry;
        data = typedEntry.Data;
        return true;
    }
    
    public bool HasData<T>()
        => _entries.TryGetValue(typeof(T), out var entry) && entry.IsSet;
}