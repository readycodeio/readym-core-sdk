namespace ReadyM.Api.Saves;

public interface ISaveSerializable
{
    void WriteSave(ISaveWriter writer, ISaveWriteContext context);
    void ReadSave(ISaveReader reader, ISaveLoadContext context);
}
