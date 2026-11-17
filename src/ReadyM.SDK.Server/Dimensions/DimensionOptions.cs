using ReadyM.Api.Idents;

namespace ReadyM.SDK.Server.Dimensions;

/// <summary>How a dimension behaves. Given when it is created and fixed from then on.</summary>
public sealed record DimensionOptions
{
    /// <summary>Required when <see cref="Persistent"/>: the save's folder, and how a later create finds it.</summary>
    public string? Name { get; init; }

    /// <summary>Where players are moved after leaving dimension areas. The default dimension unless given.</summary>
    public DimensionId Parent { get; init; }

    /// <summary>The areas it covers; null covers every area.</summary>
    public IReadOnlySet<AreaId>? Areas { get; init; }

    /// <summary>Saved, and loaded back when created again with the same name.</summary>
    public bool Persistent { get; init; }

    /// <summary>Whether a new area or cell runs the registered seeders; off leaves it empty for a mod to fill.</summary>
    public bool Seed { get; init; } = true;

    /// <summary>Destroyed when it becomes empty: no player in it, no child dimension, after a player has been in it.</summary>
    public bool DestroyWhenEmpty { get; init; }
}
