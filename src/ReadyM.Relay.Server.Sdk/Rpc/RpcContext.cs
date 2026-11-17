using ReadyM.Api.Idents;

namespace ReadyM.Relay.Server.Sdk.Rpc;

/// <summary>
/// Contextual information passed to every generated <c>OnX</c> server RPC stub.
/// Wraps per-message metadata so handler signatures stay clean as the context grows.
/// </summary>
public readonly struct RpcContext(PlayerId sender, DimensionId dimension = default)
{
    /// <summary>The client player who sent this RPC.</summary>
    public PlayerId Sender { get; } = sender;

    /// <summary>Where the sender is: their area's dimension, else theirs. Pass it to lookups that should see it.</summary>
    public DimensionId Dimension { get; } = dimension;
}