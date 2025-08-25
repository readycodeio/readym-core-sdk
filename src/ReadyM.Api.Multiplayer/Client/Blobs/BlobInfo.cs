namespace ReadyM.Api.Multiplayer.Client.Blobs;

public class BlobInfo(string name, byte[] content)
{
    public string Name { get; } = name;
    public byte[] Content { get; } = content;
}