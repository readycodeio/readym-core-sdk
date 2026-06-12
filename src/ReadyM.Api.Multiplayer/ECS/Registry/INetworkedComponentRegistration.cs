using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

// TODO: Not public
public interface INetworkedComponentRegistration : IComponentRegistrationBase<INetworkedComponentRegistry, INetworkedComponent>;