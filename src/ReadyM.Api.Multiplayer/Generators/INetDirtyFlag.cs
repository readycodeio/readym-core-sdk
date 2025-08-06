namespace ReadyM.Api.Multiplayer.Generators;

public interface INetDirtyFlag
{
    bool IsDirty { get; }
    void ClearDirty();
}