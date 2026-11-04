using System.Diagnostics.CodeAnalysis;
using LiteNetLib;
using ReadyM.Api.Multiplayer.Client;

namespace ReadyM.Api.Multiplayer.Protocol;

internal delegate bool CanConnectDelegate(RelayConnectionOptions options, ConnectionRequest request, [NotNullWhen(false)] out DisconnectedReason? reason);