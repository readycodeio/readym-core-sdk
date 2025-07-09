using System;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Relay.Client;

public readonly ref struct CustomEventEntry
{
    private readonly IRelayClient _owner;
    private readonly int _minEventCode;
    private readonly int _maxEventCode;
        
    internal CustomEventEntry(IRelayClient owner, byte minEventCode, byte maxEventCode)
    {
        _owner = owner;
        if (_minEventCode < (int)SystemEvent.MinCustomEvent || _maxEventCode > (int)SystemEvent.MaxCustomEvent)
        {
            throw new ArgumentOutOfRangeException(nameof(minEventCode), "Event codes must be between MinCustomEvent and MaxCustomEvent");
        }
        if (minEventCode > maxEventCode)
        {
            throw new ArgumentException("Min event code cannot be greater than max event code", nameof(minEventCode));
        }
        _minEventCode = minEventCode;
        _maxEventCode = maxEventCode;
    }
        
    public event Action<CustomEventHeader, NetDataReader>? OnCustomEvent
    {
        add
        {
            for (var eventCode = _minEventCode; eventCode <= _maxEventCode; eventCode++)
            {
                _owner.AddCustomEventHandler(eventCode, value);
            }
        }
        remove
        {
            for (var eventCode = _minEventCode; eventCode <= _maxEventCode; eventCode++)
            {
                _owner.RemoveCustomEventHandler(eventCode, value);
            }
        }
    }
}