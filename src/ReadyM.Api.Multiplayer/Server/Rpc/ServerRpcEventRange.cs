using System;

namespace ReadyM.Api.Multiplayer.Server.Rpc;

public readonly struct ServerRpcEventRange
{
    private readonly ServerRpcEventEntry _minEventCode;
    private readonly ServerRpcEventEntry _maxEventCode;

    public byte MinEventCode => _minEventCode.EventCode;
    public byte MaxEventCode => _maxEventCode.EventCode;

    public ServerRpcEventRange(byte minEventCode, byte maxEventCode)
        : this(new ServerRpcEventEntry(minEventCode), new ServerRpcEventEntry(maxEventCode)) { }

    public ServerRpcEventRange(ServerRpcEventEntry minEventCode, ServerRpcEventEntry maxEventCode)
    {
        if (minEventCode > maxEventCode)
        {
            throw new ArgumentException("Min event code cannot be greater than max event code", nameof(minEventCode));
        }
        _minEventCode = minEventCode;
        _maxEventCode = maxEventCode;
    }
}
