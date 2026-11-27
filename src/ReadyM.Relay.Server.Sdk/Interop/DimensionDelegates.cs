using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Native.Container;

namespace ReadyM.Relay.Server.Sdk.Interop;

/// <summary>A dimension's options as they cross to the host.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DimensionOptionsData
{
    public NativeString256 Name;
    public byte HasName;
    public DimensionId Parent;

    /// <summary>The areas it covers; <see cref="AreaCount"/> -1 covers every area.</summary>
    public AreaId* Areas;
    public int AreaCount;
    public byte Persistent;
    public byte Seed;
    public byte DestroyWhenEmpty;
}

/// <summary>What a dimension call answers; anything but <see cref="Done"/> is a refusal.</summary>
internal enum DimensionCallResult : byte
{
    Done,
    NotLive,
    NameTaken,
    NameRequired,
    ParentNotLive,
    HasChildren,
    IsMain,
    Destroying,
    NotConnected,
}

internal unsafe delegate DimensionCallResult CreateDimensionDelegate(DimensionOptionsData* options, DimensionId* created);

internal delegate DimensionCallResult DestroyDimensionDelegate(DimensionId dimension);

internal delegate DimensionCallResult SetPlayerDimensionDelegate(PlayerId player, DimensionId dimension);

internal delegate byte DimensionExistsDelegate(DimensionId dimension);

internal unsafe delegate byte TryFindDimensionDelegate(NativeString256 name, DimensionId* found);

/// <summary>The player's dimension; the main one for a player who is not connected.</summary>
internal delegate DimensionId PlayerDimensionDelegate(PlayerId player);

/// <summary>The player's current dimension: their area's, else theirs.</summary>
internal delegate DimensionId CurrentDimensionDelegate(PlayerId player);

/// <summary>The entity's dimension; Shared for one in no dimension or gone.</summary>
internal delegate DimensionId EntityDimensionDelegate(RawEntity entity);

/// <summary>1 when the dimension is the entity's origin; 0 otherwise, or for an entity that is gone.</summary>
internal delegate byte IsEntityOriginDimensionDelegate(RawEntity entity, DimensionId dimension);

/// <summary>The dimension an entity from <paramref name="from"/> enters <paramref name="area"/> in.</summary>
internal delegate DimensionId ResolveAreaDelegate(DimensionId from, AreaId area);
