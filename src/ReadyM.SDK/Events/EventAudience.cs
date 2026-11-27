using ReadyM.Api.Idents;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Events;

/// <summary>The players an accepted networked event is sent to.</summary>
// TODO: Replace with a discriminated union when C# 15 is out
internal readonly struct EventAudience
{
    private EventAudience(EventScope scope, PlayerId player, FullAreaId area, FullCellId cell)
        => (Kind, Player, Area, Cell) = (scope, player, area, cell);

    /// <summary>Which of the audiences below this is.</summary>
    public EventScope Kind { get; }

    /// <summary>The one player, for <see cref="EventScope.Single"/>.</summary>
    public PlayerId Player { get; }

    /// <summary>The area, for <see cref="EventScope.Area"/>.</summary>
    public FullAreaId Area { get; }

    /// <summary>The cell, for <see cref="EventScope.Cell"/>.</summary>
    public FullCellId Cell { get; }

    /// <summary>Every connected player.</summary>
    public static EventAudience Global { get; } = new(EventScope.Global, default, default, default);

    /// <summary>One player.</summary>
    public static EventAudience Single(PlayerId player) => new(EventScope.Single, player, default, default);

    /// <summary>Everyone in an area.</summary>
    public static EventAudience InArea(FullAreaId area)
        => new(EventScope.Area, default, area, default);

    /// <summary>Everyone in a cell.</summary>
    public static EventAudience InCell(FullCellId cell)
        => new(EventScope.Cell, default, default, cell);
}
