using System;
using System.Collections.Generic;
using System.Threading;

namespace ReadyM.Api.Helpers;

public sealed class DataSideChannel
{
    private class EntryBase
    {
        public bool IsSet;
    }

    private class Entry<T> : EntryBase
    {
        public T? Data;
    }

    public readonly struct Scope<T> : IDisposable
    {
        private readonly ThreadEntry _threadEntry;

        internal Scope(ThreadEntry threadEntry, T data)
        {
            _threadEntry = threadEntry;
            _threadEntry.PushData(data);
        }

        public void Dispose()
        {
            _threadEntry.PopData<T>();
        }
    }

    internal readonly struct ThreadEntry()
    {
        private readonly Dictionary<Type, EntryBase> _typeEntries = new();

        public void PushData<T>(T data)
        {
            if (!_typeEntries.TryGetValue(typeof(T), out var typeEntry))
            {
                typeEntry = new Entry<T> { Data = data };
                _typeEntries.Add(typeof(T), typeEntry);
            }

            if (typeEntry.IsSet)
                throw new InvalidOperationException($"Data of type {typeof(T)} is already set in the side channel.");

            typeEntry.IsSet = true;
            var typedEntry = (Entry<T>)typeEntry;
            typedEntry.Data = data;
        }

        public void PopData<T>()
        {
            if (!_typeEntries.TryGetValue(typeof(T), out var typeEntry) || !typeEntry.IsSet)
                throw new InvalidOperationException($"Data of type {typeof(T)} is not set in the side channel.");

            typeEntry.IsSet = false;
        }

        public T? GetData<T>()
        {
            if (!_typeEntries.TryGetValue(typeof(T), out var typeEntry) || !typeEntry.IsSet)
                throw new InvalidOperationException($"Data of type {typeof(T)} is not set in the side channel.");

            var typedEntry = (Entry<T>)typeEntry;
            return typedEntry.Data;
        }

        public bool TryGetData<T>(out T? data)
        {
            if (!_typeEntries.TryGetValue(typeof(T), out var typeEntry) || !typeEntry.IsSet)
            {
                data = default;
                return false;
            }

            var typedEntry = (Entry<T>)typeEntry;
            data = typedEntry.Data;
            return true;
        }

        public bool HasData<T>()
            => _typeEntries.TryGetValue(typeof(T), out var typeEntry) && typeEntry.IsSet;
    }

    private readonly ThreadLocal<ThreadEntry> _threadEntries = new(() => new ThreadEntry());

    public Scope<T> PushScope<T>(T data = default)
        where T : struct
        => new(_threadEntries.Value, data);

    public void PushData<T>(T data)
        => _threadEntries.Value.PushData(data);

    public void PopData<T>()
        => _threadEntries.Value.PopData<T>();

    public T? GetData<T>()
        => _threadEntries.Value.GetData<T>();

    public bool TryGetData<T>(out T? data)
        => _threadEntries.Value.TryGetData<T>(out data);

    public bool HasData<T>()
        => _threadEntries.Value.HasData<T>();
}
