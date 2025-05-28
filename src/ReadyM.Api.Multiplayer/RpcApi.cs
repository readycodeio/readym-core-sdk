using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Api.Multiplayer;

public partial class ReadyMultiplayerMod
{
    private record struct RpcInfo(RelayMode RelayMode, Action<NetDataReader> OnReceived);

    private readonly Dictionary<byte, RpcInfo> _rpcEvents = new();

    private class RpcConfig<TCode>(ReadyMultiplayerMod mod) : IRpcConfiguration<TCode> where TCode : Enum
    {
        public IRpcConfiguration<TCode> DefineEvent<T>(TCode code, RelayMode relayMode, Action<T> onReceived)
        {
            var enumValue = Convert.ToByte(code);
            mod._rpcEvents.Add(enumValue, new RpcInfo(relayMode, reader =>
            {
                var payload = mod.RelayClient.DeserializeObject<T>(reader);
                onReceived(payload);
            }));

            return this;
        }
    }

    #region RPC

    private Type? _rpcEnumType;

    public void RegisterRpcEvents<T>(Action<IRpcConfiguration<T>> configure) where T : Enum
    {
        if (_rpcEnumType is not null)
            throw new InvalidOperationException("RPC events have already been registered.");

        _rpcEnumType = typeof(T);
        var config = new RpcConfig<T>(this);
        configure(config);
    }

    #endregion
}