using ReadyM.Api.Multiplayer.Serialization;
using ReadyM.Relay.Client.Shim.ECS;

namespace ReadyM.Relay.Client.Serialization;

public class ClientShimTextSerializerRegistration : ITextRelaySerializerRegistration
{
    public void Register(TextRelaySerializer serializer)
    {
        serializer.RegisterPolymorphicType<ShimEcsDependencyData>("shimEcs");
    }
}