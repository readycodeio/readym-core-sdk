using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Commands;
using Xunit;

namespace ReadyM.SDK.Tests.Commands;

public sealed class CommandDispatcherTests
{
    private sealed class Captured : ICommandOutput
    {
        public List<string> Replies { get; } = [];
        public List<string> Refusals { get; } = [];

        public void Reply(string text) => Replies.Add(text);
        public void Refuse(string text) => Refusals.Add(text);
    }

    private sealed class DenyEverything : ICommandPermissions
    {
        public bool IsAllowed(Guid? caller, string permission) => false;
    }

    private static (CommandRegistry Registry, CommandDispatcher Dispatcher, Captured Output) Build(
        ICommandPermissions? permissions = null)
    {
        var registry = new CommandRegistry();
        var dispatcher = new CommandDispatcher(
            registry, permissions ?? new AllowAllPermissions(), NullLogger.Instance);

        return (registry, dispatcher, new Captured());
    }

    private static CommandContext Context(Captured output, Guid? caller = null)
        => new(caller, default, "unused", output);

    [Fact]
    public void AVerbatimRemainderKeepsItsSpacingAndQuotes()
    {
        var (registry, dispatcher, output) = Build();
        string? seen = null;

        registry.Add("me", (CommandContext _, [Remainder] string action) => seen = action, "/me <action>");

        var outcome = dispatcher.Dispatch("me sits  down and says \"hello\"", Context(output));

        Assert.Equal(CommandOutcome.Ran, outcome);
        Assert.Equal("sits  down and says \"hello\"", seen);
    }

    [Fact]
    public void ARemainderIsTakenAfterTheTypedArgumentsBeforeIt()
    {
        var (registry, dispatcher, output) = Build();
        var id = 0;
        string? message = null;

        registry.Add("dm", (CommandContext _, int target, [Remainder] string text) =>
        {
            id = target;
            message = text;
        }, "/dm <id> <text>");

        Assert.Equal(CommandOutcome.Ran, dispatcher.Dispatch("dm 7 two  spaces", Context(output)));
        Assert.Equal(7, id);
        Assert.Equal("two  spaces", message);
    }

    [Fact]
    public void AMissingRemainderIsRefusedWithTheUsageRatherThanRunWithNothing()
    {
        var (registry, dispatcher, output) = Build();
        var ran = false;

        registry.Add("me", (CommandContext _, [Remainder] string action) => ran = true, "/me <action>");

        Assert.Equal(CommandOutcome.BadArguments, dispatcher.Dispatch("me", Context(output)));
        Assert.False(ran);
        Assert.Contains("/me <action>", output.Refusals[0]);
    }

    [Fact]
    public void AnArgumentOfTheWrongTypeSaysWhichOne()
    {
        var (registry, dispatcher, output) = Build();

        registry.Add("dm", (CommandContext _, int target, [Remainder] string text) => { }, "/dm <id> <text>");

        Assert.Equal(CommandOutcome.BadArguments, dispatcher.Dispatch("dm notanumber hello", Context(output)));
        Assert.Single(output.Refusals);
    }

    [Fact]
    public void AnUnknownCommandIsSaidToBeUnknown()
    {
        var (_, dispatcher, output) = Build();

        Assert.Equal(CommandOutcome.Unknown, dispatcher.Dispatch("nosuch", Context(output)));
        Assert.Contains("/nosuch", output.Refusals[0]);
    }

    [Fact]
    public void AHandlerThatThrowsIsContainedAndReported()
    {
        var (registry, dispatcher, output) = Build();

        registry.Add("boom", (CommandContext _) => { throw new InvalidOperationException("no"); }, "/boom");

        Assert.Equal(CommandOutcome.Threw, dispatcher.Dispatch("boom", Context(output)));
        Assert.Single(output.Refusals);
    }

    [Fact]
    public void ARestrictedCommandIsRefusedBeforeItsHandlerRuns()
    {
        var (registry, dispatcher, output) = Build(new DenyEverything());
        var ran = false;

        registry.Add("kick", (CommandContext _) => ran = true, "/kick", permission: "command.kick");

        Assert.Equal(CommandOutcome.Denied, dispatcher.Dispatch("kick", Context(output)));
        Assert.False(ran);
    }

    [Fact]
    public void AnUnrestrictedCommandNeverAsksAboutPermissions()
    {
        var (registry, dispatcher, output) = Build(new DenyEverything());
        var ran = false;

        registry.Add("who", (CommandContext _) => ran = true, "/who");

        Assert.Equal(CommandOutcome.Ran, dispatcher.Dispatch("who", Context(output)));
        Assert.True(ran);
    }

    [Fact]
    public void AHandlerWithoutAContextFirstIsRefusedAtRegistration()
    {
        var (registry, _, _) = Build();

        Assert.Throws<ArgumentException>(() => registry.Add("bad", (int x) => { }, "/bad"));
    }

    [Fact]
    public void ARemainderThatIsNotLastIsRefusedAtRegistration()
    {
        var (registry, _, _) = Build();

        Assert.Throws<ArgumentException>(
            () => registry.Add("bad", (CommandContext _, [Remainder] string a, int b) => { }, "/bad"));
    }

    [Fact]
    public void TheSameNameTwiceIsRefused()
    {
        var (registry, _, _) = Build();

        registry.Add("me", (CommandContext _) => { }, "/me");

        Assert.Throws<ArgumentException>(() => registry.Add("ME", (CommandContext _) => { }, "/me"));
    }
}
