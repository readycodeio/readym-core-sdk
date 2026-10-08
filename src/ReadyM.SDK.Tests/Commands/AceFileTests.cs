using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.SDK.Server.Commands;
using Xunit;

namespace ReadyM.SDK.Tests.Commands;

public sealed class AceFileTests : IDisposable
{
    private static readonly Guid Player = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "readym-acefile-" + Guid.NewGuid().ToString("N"));

    private string Path_ => Path.Combine(_directory, AceFile.FileName);

    public AceFileTests() => Directory.CreateDirectory(_directory);

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

    private static AceDocument Sample() => new()
    {
        Principals = { [Player.ToString("D")] = ["group.admin"] },
        Inherits = { ["group.admin"] = ["group.mod"] },
        Aces = { new AceEntry { Principal = "group.mod", Object = "command.kick", Allow = true } },
    };

    [Fact]
    public void AnAbsentFileReadsAsNothingRatherThanThrowing()
    {
        Assert.Null(AceFile.Read(Path_));
    }

    [Fact]
    public void WhatIsWrittenComesBackTheSame()
    {
        AceFile.Write(Path_, Sample());

        var read = AceFile.Read(Path_);

        Assert.NotNull(read);
        Assert.Equal(["group.admin"], read!.Principals[Player.ToString("D")]);
        Assert.Equal(["group.mod"], read.Inherits["group.admin"]);
        Assert.Equal("command.kick", Assert.Single(read.Aces).Object);
        Assert.True(read.Aces[0].Allow);
    }

    [Fact]
    public void AWriteLeavesNoTemporaryFileBehind()
    {
        AceFile.Write(Path_, Sample());

        Assert.Equal([AceFile.FileName], Directory.GetFiles(_directory).Select(Path.GetFileName));
    }

    [Fact]
    public void AWriteOverAnExistingFileReplacesIt()
    {
        AceFile.Write(Path_, Sample());
        AceFile.Write(Path_, new AceDocument());

        var read = AceFile.Read(Path_);

        Assert.NotNull(read);
        Assert.Empty(read!.Aces);
        Assert.Empty(read.Principals);
    }

    [Fact]
    public void AWriteReachesAServerAlreadyWatchingTheFile()
    {
        using var permissions = new AcePermissions(Path_, NullLogger.Instance);
        Assert.False(permissions.IsAllowed(Player, "command.kick"));

        AceFile.Write(Path_, Sample());

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline && !permissions.IsAllowed(Player, "command.kick"))
            Thread.Sleep(25);

        Assert.True(permissions.IsAllowed(Player, "command.kick"));
    }
}
