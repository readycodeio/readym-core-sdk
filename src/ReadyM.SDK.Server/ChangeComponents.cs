using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.SDK.Server;

internal static class ChangeComponents
{
    public static Type? Of(Type component)
        => Activator.CreateInstance(component) is INetworkedComponent networked ? networked.GetChangeComponent() : null;
}
