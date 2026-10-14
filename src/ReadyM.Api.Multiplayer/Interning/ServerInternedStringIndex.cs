using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Serialization;
using Yooni.Native.Container;
using Yooni.Native.Serialization;

namespace ReadyM.Api.Multiplayer.Interning;

/// Gives every new string the next id and queues it for the clients.
public sealed class ServerInternedStringIndex : IInternedStringIndex
{
    public const int DefaultCapacity = 65536;

    private readonly int _capacity;
    private readonly Dictionary<NativeString256, uint> _ids = new();
    private readonly List<NativeString256> _names = new();
    private int _firstPending;

    public ServerInternedStringIndex(int capacity, ILogger logger)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity cannot be negative");

        _capacity = capacity;
        _logger = logger;
    }

    private readonly ILogger _logger;
    private readonly HashSet<uint> _reported = new();
    private bool _capacityReported;

    public void ReportUnresolved(uint id)
    {
        // The server never gave this id out, so it came from a client
        if (_reported.Add(id))
            _logger.LogWarning("Interned string {Id} came from a client but was never given out; read as empty ({Count} known)", id, Count);
    }

    public int Count => _names.Count;

    public bool HasPendingAppend => _firstPending < _names.Count;

    public bool TryGetIdForWrite(in NativeString256 value, out uint id)
    {
        if (_ids.TryGetValue(value, out id))
            return true;

        // Past the cap new strings stay inline, so a field marked by mistake cannot grow the index for ever
        if (_names.Count >= _capacity)
        {
            if (!_capacityReported)
            {
                _capacityReported = true;
                _logger.LogError("The interned string index is full at {Capacity}; \"{Value}\" and every later new string stay inline. A field marked [Interned] does not hold a closed set", _capacity, value.ToString());
            }

            return false;
        }

        id = (uint)_names.Count;
        _names.Add(value);
        _ids.Add(value, id);
        return true;
    }

    public bool TryResolve(uint id, out NativeString256 value)
    {
        if (id < (uint)_names.Count)
        {
            value = _names[(int)id];
            return true;
        }

        value = default;
        return false;
    }

    /// Writes the strings added since the last append, then forgets them.
    public void WritePendingAppend(NetDataWriter writer)
    {
        Write(writer, _firstPending);
        _firstPending = _names.Count;
    }

    /// Writes every string, for a player joining.
    public void WriteAll(NetDataWriter writer)
        => Write(writer, 0);

    // [varuint first id][varuint count][strings], read by ClientInternedStringIndex.ApplyAppend
    private void Write(NetDataWriter writer, int first)
    {
        writer.PutVarUInt((uint)first);
        writer.PutVarUInt((uint)(_names.Count - first));

        for (var i = first; i < _names.Count; i++)
            _names[i].Serialize(writer);
    }
}
