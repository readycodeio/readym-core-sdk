using ReadyM.Api.Idents;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Events;

/// <summary>The players an accepted networked event is sent to.</summary>
// TODO: Replace with a discriminated union when C# 15 is out
internal readonly struct EventAudience
{
    private EventAudience(EventScope scope, PlayerId player, AreaId area, FullCellId cell, DimensionId dimension)
        => (Kind, Player, Area, Cell, Dimension) = (scope, player, area, cell, dimension);

    /// <summary>Which of the audiences below this is.</summary>
    public EventScope Kind { get; }

    /// <summary>The one player, for <see cref="EventScope.Single"/>.</summary>
    public PlayerId Player { get; }

    /// <summary>The area, for <see cref="EventScope.Area"/>.</summary>
    public AreaId Area { get; }

    /// <summary>The cell, for <see cref="EventScope.Cell"/>.</summary>
    public FullCellId Cell { get; }

    /// <summary>The dimension of the area or cell, on the server; <see cref="DimensionId.Invalid"/> on a client and for the others.</summary>
    public DimensionId Dimension { get; }

    /// <summary>Every connected player.</summary>
    public static EventAudience Global { get; } = new(EventScope.Global, default, default, default, DimensionId.Invalid);

    /// <summary>One player.</summary>
    public static EventAudience Single(PlayerId player) => new(EventScope.Single, player, default, default, DimensionId.Invalid);

    /// <summary>Everyone in an area.</summary>
    public static EventAudience InArea(AreaId area, DimensionId dimension)
        => new(EventScope.Area, default, area, default, dimension);

    /// <summary>Everyone in a cell.</summary>
    public static EventAudience InCell(FullCellId cell, DimensionId dimension)
        => new(EventScope.Cell, default, default, cell, dimension);
}
