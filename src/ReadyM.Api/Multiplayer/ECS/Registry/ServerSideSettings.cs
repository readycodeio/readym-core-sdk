namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal class ServerSideSettings(bool isServerSide)
{
    public readonly bool IsServerSide = isServerSide;

    public static ServerSideSettings Client()
        => new(false);

    public static ServerSideSettings ServerSide()
        => new(true);
}
