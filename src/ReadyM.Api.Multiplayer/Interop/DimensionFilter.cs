using System;
using System.Runtime.InteropServices;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Interop;

/// <summary>How many dimensions a <see cref="DimensionFilter"/> sees. Shared entities are seen by both.</summary>
internal enum DimensionScope : byte
{
    /// <summary>Every dimension: what a mod that names none gets.</summary>
    Every = 0,

    /// <summary>The one dimension the filter names.</summary>
    One = 1,
}

/// <summary>
/// Which dimensions a mod's query, lookup or player list sees: every dimension, or one dimension. Shared entities are
/// seen either way.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct DimensionFilter
{
    private DimensionFilter(DimensionScope scope, DimensionId dimension)
    {
        Scope = scope;
        Dimension = dimension;
    }

    /// <summary>Every dimension's entities, and the shared ones.</summary>
    public static DimensionFilter EveryDimension { get; } = new(DimensionScope.Every, DimensionId.Main);

    /// <summary>One dimension's entities, and the shared ones.</summary>
    public static DimensionFilter InDimension(DimensionId dimension) => new(DimensionScope.One, dimension);

    public DimensionScope Scope { get; }

    /// <summary>The dimension an <see cref="InDimension"/> filter names; not used while it sees every dimension.</summary>
    public DimensionId Dimension { get; }

    public bool IsEveryDimension => Scope == DimensionScope.Every;

    public bool Sees(DimensionId dimension) => Scope switch
    {
        DimensionScope.Every => true,
        DimensionScope.One => dimension == Dimension || dimension.IsShared,
        _ => throw new ArgumentOutOfRangeException(nameof(Scope), Scope, "Unknown dimension scope"),
    };
}
