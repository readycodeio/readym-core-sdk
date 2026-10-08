using System;
using ReadyM.Api.Idents;

namespace ReadyM.SDK.Commands;

public sealed class CommandContext(Guid? caller, PlayerId player, string rawLine, ICommandOutput output)
{
    public Guid? Caller { get; } = caller;

    public PlayerId Player { get; } = player;

    public string RawLine { get; } = rawLine;

    public ICommandOutput Output { get; } = output;
}
