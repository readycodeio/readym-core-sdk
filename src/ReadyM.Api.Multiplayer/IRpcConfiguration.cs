using System;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Api.Multiplayer;

public interface IRpcConfiguration<in TCode> where TCode : Enum
{
    IRpcConfiguration<TCode> DefineEvent<T>(TCode code, RelayMode relayMode, Action<T> onReceived);
}