namespace ReadyM.Api.Multiplayer.Serialization;

public interface ISaveWriter
{
    void BeginObject();
    void EndObject();

    /// <summary>
    /// Opens an array. <paramref name="count"/> is a hint a length-prefixed encoder can write up front; a
    /// self-delimiting encoder (JSON) may ignore it.
    /// </summary>
    void BeginArray(int count);
    void EndArray();

    /// <summary>Names the next value written into the current object.</summary>
    void Name(string name);

    void Write(bool value);
    void Write(byte value);
    void Write(sbyte value);
    void Write(short value);
    void Write(ushort value);
    void Write(int value);
    void Write(uint value);
    void Write(long value);
    void Write(ulong value);
    void Write(float value);
    void Write(double value);
    void Write(string value);
}
