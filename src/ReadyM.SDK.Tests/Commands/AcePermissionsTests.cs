using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.SDK.Server.Commands;
using Xunit;

namespace ReadyM.SDK.Tests.Commands;

public sealed class AcePermissionsTests : IDisposable
{
    private static readonly Guid Player = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "readym-ace-" + Guid.NewGuid().ToString("N"));

    private string File => Path.Combine(_directory, "permissions.json");

    public AcePermissionsTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private AcePermissions Open() => new(File, NullLogger.Instance);

    private void WriteFile(string json) => System.IO.File.WriteAllText(File, json);

    [Fact]
    public void WithNoFileOnlyWhatCodeGrantedIsInForce()
    {
        using var permissions = Open();

        Assert.False(permissions.IsAllowed(Player, "command.kick"));

        permissions.Allow(Player.ToString("D"), "command.kick");

        Assert.True(permissions.IsAllowed(Player, "command.kick"));
    }

    [Fact]
    public void ACallerWithNoReadyMIdHoldsNothing()
    {
        using var permissions = Open();
        permissions.Allow(Player.ToString("D"), "command.kick");

        Assert.False(permissions.IsAllowed(null, "command.kick"));
    }

    [Fact]
    public void TheFileIsReadWhenThePermissionsOpen()
    {
        WriteFile($$"""
        {
          "aces": [ { "principal": "{{Player:D}}", "object": "command", "allow": true } ]
        }
        """);

        using var permissions = Open();

        Assert.True(permissions.IsAllowed(Player, "command.kick"));
    }

    [Fact]
    public void ADenyInCodeOverridesAGrantInTheFile()
    {
        WriteFile($$"""
        {
          "aces": [ { "principal": "{{Player:D}}", "object": "command.kick", "allow": true } ]
        }
        """);

        using var permissions = Open();
        permissions.Deny(Player.ToString("D"), "command");

        Assert.False(permissions.IsAllowed(Player, "command.kick"));
    }

    [Fact]
    public void AGroupFromCodeMeetsItsGrantFromTheFile()
    {
        WriteFile("""
        {
          "aces": [ { "principal": "group.admin", "object": "command.kick", "allow": true } ]
        }
        """);

        using var permissions = Open();
        permissions.AddToGroup(Player.ToString("D"), "group.admin");

        Assert.True(permissions.IsAllowed(Player, "command.kick"));
    }

    [Fact]
    public void AnUnreadableFileLeavesWhatWasAlreadyLoadedAlone()
    {
        WriteFile($$"""
        {
          "aces": [ { "principal": "{{Player:D}}", "object": "command.kick", "allow": true } ]
        }
        """);

        using var permissions = Open();
        Assert.True(permissions.IsAllowed(Player, "command.kick"));

        WriteFile("{ not json");
        Thread.Sleep(750);

        Assert.True(permissions.IsAllowed(Player, "command.kick"));
    }

    [Fact]
    public void AnEditedFileIsPickedUpWithoutARestart()
    {
        WriteFile("{ }");

        using var permissions = Open();
        Assert.False(permissions.IsAllowed(Player, "command.kick"));

        WriteFile($$"""
        {
          "aces": [ { "principal": "{{Player:D}}", "object": "command.kick", "allow": true } ]
        }
        """);

        Assert.True(WaitUntil(() => permissions.IsAllowed(Player, "command.kick")));
    }

    [Fact]
    public void AReloadKeepsWhatModsGrantedInCode()
    {
        WriteFile("{ }");

        using var permissions = Open();
        permissions.Allow(Player.ToString("D"), "command.kick");

        WriteFile("""
        {
          "aces": [ { "principal": "group.admin", "object": "command.ban", "allow": true } ]
        }
        """);

        Assert.True(WaitUntil(() => permissions.IsAllowed(Player, "command.kick")));
    }

    private static bool WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);

        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return true;
            Thread.Sleep(25);
        }

        return condition();
    }
}
