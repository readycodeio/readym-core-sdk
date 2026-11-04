using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal interface INetworkedComponentRegistration : IComponentRegistrationBase<INetworkedComponentRegistry, INetworkedComponent>;