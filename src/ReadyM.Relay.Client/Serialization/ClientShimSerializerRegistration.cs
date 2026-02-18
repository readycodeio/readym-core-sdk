using ReadyM.Relay.Client.Shim.ECS;
using ReadyM.Relay.Common.Serialization;

namespace ReadyM.Relay.Client.Serialization;

public class ClientShimTextSerializerRegistration : ITextRelaySerializerRegistration
{
    public void Register(TextRelaySerializer serializer)
    {
        serializer.RegisterPolymorphicType<ShimEcsDependencyData>("shimEcs");
    }
}