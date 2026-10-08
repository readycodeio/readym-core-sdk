namespace ReadyM.SDK.Commands;

public interface ICommandOutput
{
    void Reply(string text);

    void Refuse(string text);
}
