using System;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Client;

public readonly ref struct CustomEventEntry
{
    private readonly IRelayClient _owner;
    private readonly int _minEventCode;
    private readonly int _maxEventCode;
        
    public CustomEventEntry(IRelayClient owner, byte minEventCode, byte maxEventCode)
    {
        _owner = owner;
        if (_minEventCode < (int)RelayMessageCode.MinCustomEvent || _maxEventCode > (int)RelayMessageCode.MaxCustomEvent)
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