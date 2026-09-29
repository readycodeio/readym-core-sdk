namespace ReadyM.Api.Saves;

public interface ISavableComponent : ISaveSerializable
{
    string SaveName { get; }
    ushort SaveVersion { get; }
}
