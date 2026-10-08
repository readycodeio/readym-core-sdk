using ReadyM.SDK.Server.Commands;
using Xunit;

namespace ReadyM.SDK.Tests.Commands;

public sealed class AceTableTests
{
    private const string Player = "11111111-1111-1111-1111-111111111111";

    private static AceTable Table(AceDocument document) => new(document);

    private static AceEntry Ace(string principal, string permission, bool allow)
        => new() { Principal = principal, Object = permission, Allow = allow };

    [Fact]
    public void NothingIsAllowedUntilSomethingSaysSo()
    {
        Assert.False(AceTable.Empty.IsAllowed(Player, "command.kick"));
    }

    [Fact]
    public void AGrantOnTheExactObjectLetsThatPrincipalThrough()
    {
        var table = Table(new AceDocument { Aces = { Ace(Player, "command.kick", allow: true) } });

        Assert.True(table.IsAllowed(Player, "command.kick"));
        Assert.False(table.IsAllowed(Player, "command.ban"));
    }

    [Fact]
    public void AGrantOnAParentCoversTheObjectsUnderIt()
    {
        var table = Table(new AceDocument { Aces = { Ace(Player, "command", allow: true) } });

        Assert.True(table.IsAllowed(Player, "command.kick"));
        Assert.True(table.IsAllowed(Player, "command.kick.silent"));
    }

    [Fact]
    public void ADenyBeatsAGrantHoweverSpecificTheGrantIs()
    {
        var table = Table(new AceDocument
        {
            Aces =
            {
                Ace(Player, "command.kick", allow: true),
                Ace(Player, "command", allow: false),
            },
        });

        Assert.False(table.IsAllowed(Player, "command.kick"));
    }

    [Fact]
    public void AGroupsGrantReachesItsMembers()
    {
        var table = Table(new AceDocument
        {
            Principals = { [Player] = ["group.admin"] },
            Aces = { Ace("group.admin", "command.kick", allow: true) },
        });

        Assert.True(table.IsAllowed(Player, "command.kick"));
    }

    [Fact]
    public void AGroupInheritsFromAnotherWithoutLoopingForever()
    {
        var table = Table(new AceDocument
        {
            Principals = { [Player] = ["group.mod"] },
            Inherits =
            {
                ["group.mod"] = ["group.admin"],
                ["group.admin"] = ["group.mod"],
            },
            Aces = { Ace("group.admin", "command.kick", allow: true) },
        });

        Assert.True(table.IsAllowed(Player, "command.kick"));
    }
}
