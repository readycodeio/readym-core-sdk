using System.Runtime.InteropServices;
using ReadyM.Api.Idents;
using ReadyM.Native.Container;
using ReadyM.Relay.Server.Sdk.Interop;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Server.Dimensions;

/// <summary>The dimensions, asked of the host; every refusal comes back as the exception the host's service throws.</summary>
internal sealed class DimensionApi : IDimensions
{
    private readonly CreateDimensionDelegate _create;
    private readonly DestroyDimensionDelegate _destroy;
    private readonly SetPlayerDimensionDelegate _setPlayerDimension;
    private readonly DimensionExistsDelegate _exists;
    private readonly TryFindDimensionDelegate _tryFind;
    private readonly PlayerDimensionDelegate _playerDimension;
    private readonly CurrentDimensionDelegate _currentDimension;
    private readonly EntityDimensionDelegate _entityDimension;

    internal DimensionApi(DimensionApiPointers pointers)
    {
        _create = Marshal.GetDelegateForFunctionPointer<CreateDimensionDelegate>(pointers.Create);
        _destroy = Marshal.GetDelegateForFunctionPointer<DestroyDimensionDelegate>(pointers.Destroy);
        _setPlayerDimension = Marshal.GetDelegateForFunctionPointer<SetPlayerDimensionDelegate>(pointers.SetPlayerDimension);
        _exists = Marshal.GetDelegateForFunctionPointer<DimensionExistsDelegate>(pointers.Exists);
        _tryFind = Marshal.GetDelegateForFunctionPointer<TryFindDimensionDelegate>(pointers.TryFind);
        _playerDimension = Marshal.GetDelegateForFunctionPointer<PlayerDimensionDelegate>(pointers.PlayerDimension);
        _currentDimension = Marshal.GetDelegateForFunctionPointer<CurrentDimensionDelegate>(pointers.CurrentDimension);
        _entityDimension = Marshal.GetDelegateForFunctionPointer<EntityDimensionDelegate>(pointers.EntityDimension);
    }

    /// <summary>The entity's dimension by its raw identity, for the entity api's own lookups.</summary>
    internal DimensionId EntityDimensionOf(Friflo.Engine.ECS.RawEntity entity) => _entityDimension(entity);

    public bool Exists(DimensionId id) => _exists(id) != 0;

    public unsafe bool TryFind(string name, out DimensionId id)
    {
        DimensionId found;
        var ok = _tryFind(new NativeString256(name, useWide: false), &found) != 0;
        id = found;
        return ok;
    }

    public unsafe DimensionId Create(DimensionOptions options)
    {
        var areas = options.Areas is { } set ? set.ToArray() : null;

        fixed (AreaId* areaPointer = areas)
        {
            var data = new DimensionOptionsData
            {
                Name = new NativeString256(options.Name ?? string.Empty, useWide: false),
                HasName = options.Name is null ? (byte)0 : (byte)1,
                Parent = options.Parent,
                Areas = areaPointer,
                AreaCount = areas?.Length ?? -1,
                Persistent = options.Persistent ? (byte)1 : (byte)0,
                Seed = options.Seed ? (byte)1 : (byte)0,
                DestroyWhenEmpty = options.DestroyWhenEmpty ? (byte)1 : (byte)0,
            };

            DimensionId created;
            Check(_create(&data, &created), options.Name ?? "a dimension");
            return created;
        }
    }

    public void Destroy(DimensionId id) => Check(_destroy(id), id.ToString());

    public DimensionId GetPlayerDimension(PlayerId player) => _playerDimension(player);

    public DimensionId GetCurrentDimension(PlayerId player) => _currentDimension(player);

    public DimensionId GetEntityDimension(EntityHandle entity) => _entityDimension(entity.RawEntity);

    public unsafe DimensionSwitchMode SetPlayerDimension(PlayerId player, DimensionId id)
    {
        byte outcome;
        Check(_setPlayerDimension(player, id, &outcome), $"{player} to {id}");
        return (DimensionSwitchMode)outcome;
    }

    private static void Check(DimensionCallResult result, string what)
    {
        switch (result)
        {
            case DimensionCallResult.Done:
                return;
            case DimensionCallResult.NotLive:
                throw new KeyNotFoundException($"The dimension is not live ({what}).");
            case DimensionCallResult.NotConnected:
                throw new KeyNotFoundException($"The player is not connected ({what}).");
            case DimensionCallResult.NameRequired:
                throw new ArgumentException($"A persistent dimension needs a name ({what}).");
            case DimensionCallResult.NotSupported:
                throw new NotSupportedException($"A persistent dimension is not built yet ({what}).");
            case DimensionCallResult.NameTaken:
                throw new InvalidOperationException($"A dimension with this name is live ({what}).");
            case DimensionCallResult.ParentNotLive:
                throw new InvalidOperationException($"The parent dimension is not live ({what}).");
            case DimensionCallResult.HasChildren:
                throw new InvalidOperationException($"The dimension has child dimensions; destroy them first ({what}).");
            case DimensionCallResult.IsMain:
                throw new InvalidOperationException($"The main dimension cannot be destroyed ({what}).");
            case DimensionCallResult.Destroying:
                throw new InvalidOperationException($"The dimension is being destroyed ({what}).");
            default:
                throw new ArgumentOutOfRangeException(nameof(result), result, "Unknown dimension call result.");
        }
    }
}
