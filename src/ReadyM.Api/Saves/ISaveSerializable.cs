namespace ReadyM.Api.Saves;

public interface ISaveSerializable
{
    void WriteSave(ISaveWriter writer);
    void ReadSave(ISaveReader reader);
}
