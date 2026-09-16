namespace ReadyM.Api.Saves;

public interface ISaveReader
{
    void BeginObject();
    void EndObject();

    void BeginArray();

    /// <summary>True while the current array has another element to read.</summary>
    bool HasMoreElements();
    void EndArray();

    /// <summary>Selects the named field in the current object by name.</summary>
    void Name(string name);

    bool ReadBool();
    byte ReadByte();
    sbyte ReadSByte();
    short ReadShort();
    ushort ReadUShort();
    int ReadInt();
    uint ReadUInt();
    long ReadLong();
    ulong ReadULong();
    float ReadFloat();
    double ReadDouble();
    string ReadString();
}
