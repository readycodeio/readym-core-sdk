using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Interop;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Ecs.Components;
using ReadyM.Relay.Server.Sdk.Interop;
using ReadyM.SDK.Server.Entity;
using Yooni.Native.Container;

namespace ReadyM.SDK.TestHarness;

/// <summary>
/// Stands in for the AOT relay on the other side of the interop boundary.
/// </summary>
/// <remarks>
/// It implements the ABI contract rather than reproducing the relay: archetypes of entities, a heap
/// per component in one of the two kinds the ABI allows, and the entry points the v1 server SDK
/// binds. Everything is reached the way the real thing is reached, through function pointers, so the
/// marshalling the mod host really pays is inside the path under test. The call counters are the
/// point of the harness: they let a test assert how often the boundary was crossed.
/// </remarks>
internal sealed class FakeRelay
{
    private readonly Dictionary<string, int> _idsByName = [];
    private readonly List<Func<FakeHeap>> _heapFactories = [];
    private readonly List<FakeArchetype> _archetypes = [];
    private readonly List<EntitySlot> _entities = [default];

    // Rooted, so the GC cannot collect a delegate whose function pointer is already handed out.
    private readonly QueryDelegate _query;
    private readonly GetComponentSlotDelegate _getComponentSlot;
    private readonly IsEntityAliveDelegate _isEntityAlive;
    private readonly GetComponentIdByNameDelegate _getComponentIdByName;
    private readonly RegisterModComponentDelegate _registerModComponent;

    internal unsafe FakeRelay()
    {
        _query = QueryImpl;
        _getComponentSlot = GetComponentSlotImpl;
        _isEntityAlive = IsEntityAliveImpl;
        _getComponentIdByName = GetComponentIdByNameImpl;
        _registerModComponent = static (_, _) => -1;
    }

    internal int QueryCalls { get; private set; }

    internal int SlotCalls { get; private set; }

    internal int AliveCalls { get; private set; }

    internal void ResetCounters() => (QueryCalls, SlotCalls, AliveCalls) = (0, 0, 0);

    internal EcsApiPointers Pointers => new()
    {
        Query = Marshal.GetFunctionPointerForDelegate(_query),
        GetComponentSlot = Marshal.GetFunctionPointerForDelegate(_getComponentSlot),
        IsEntityAlive = Marshal.GetFunctionPointerForDelegate(_isEntityAlive),
        CreateNetworkedEntity = IntPtr.Zero,
        CreateNetworkedPlayerEntity = IntPtr.Zero,
        CreateNetworkedAreaEntity = IntPtr.Zero,
        CreateNetworkedCellEntity = IntPtr.Zero,
        CreateLocalEntity = IntPtr.Zero,
        DeleteNetworkedEntity = IntPtr.Zero,
        DeleteEntityTree = IntPtr.Zero,
        SetParent = IntPtr.Zero,
        GetParent = IntPtr.Zero,
        GetChildren = IntPtr.Zero
    };

    internal AotPointers Aot => new()
    {
        RegisterModComponent = Marshal.GetFunctionPointerForDelegate(_registerModComponent),
        GetComponentIdByName = Marshal.GetFunctionPointerForDelegate(_getComponentIdByName)
    };

    // -- world building, done by a test rather than across the boundary ------------------------

    /// A component the AOT side owns: blittable, pinned, reachable by address.
    internal void RegisterBlittable<T>() where T : struct => Register<T>(static () => new PinnedHeap<T>());

    /// A component a mod owns: reachable only through its heap handle, so managed fields are allowed.
    internal void RegisterManaged<T>() where T : struct => Register<T>(static () => new ManagedHeap<T>());

    private void Register<T>(Func<FakeHeap> factory) where T : struct
    {
        _idsByName[typeof(T).FullName!] = _heapFactories.Count;
        _heapFactories.Add(factory);
    }

    internal RawEntity Create(params Type[] componentTypes)
    {
        var ids = componentTypes.Select(IdOf).OrderBy(id => id).ToArray();
        var archetype = GetOrCreateArchetype(ids);
        var row = archetype.AppendRow();
        var entity = RawEntities.From(_entities.Count, 1);

        _entities.Add(new EntitySlot { Archetype = archetype, Row = row, Revision = 1, Alive = true });
        archetype.Entities[row] = entity;

        return entity;
    }

    internal void Kill(RawEntity entity) => _entities[entity.Id] = _entities[entity.Id] with { Alive = false };

    /// Hands a dead entity's id straight back out under a new revision, which is what the check is for.
    internal RawEntity Recycle(RawEntity entity)
    {
        var slot = _entities[entity.Id];
        var revision = (short)(slot.Revision + 1);
        var revived = RawEntities.From(entity.Id, revision);

        _entities[entity.Id] = slot with { Revision = revision, Alive = true };
        slot.Archetype.Entities[slot.Row] = revived;

        return revived;
    }

    internal void Set<T>(RawEntity entity, T value) where T : struct
    {
        var slot = _entities[entity.Id];
        slot.Archetype.HeapOf(IdOf(typeof(T))).SetValue(slot.Row, value);
    }

    internal T Get<T>(RawEntity entity) where T : struct
    {
        var slot = _entities[entity.Id];
        return (T)slot.Archetype.HeapOf(IdOf(typeof(T))).GetValue(slot.Row);
    }

    /// Which of the two heap kinds a component landed in, so a test can pin that it covers both.
    internal bool IsManaged<T>(RawEntity entity) where T : struct
        => _entities[entity.Id].Archetype.HeapOf(IdOf(typeof(T))) is ManagedHeap<T>;

    /// The v1 server api bound to this relay, wired the way the mod host wires it.
    internal ServerEntityApi CreateEntityApi()
        => new(Pointers, new ComponentRegistry(Aot, new ModComponentManager(), NullLogger.Instance));

    /// The id the relay assigned, for a caller that drives Query itself rather than through the SDK.
    internal int ComponentId<T>() where T : struct => IdOf(typeof(T));

    private int IdOf(Type type) => _idsByName.TryGetValue(type.FullName!, out var id)
        ? id
        : throw new InvalidOperationException($"{type.Name} was never registered with the fake relay.");

    private FakeArchetype GetOrCreateArchetype(int[] ids)
    {
        foreach (var candidate in _archetypes)
            if (candidate.ComponentIds.AsSpan().SequenceEqual(ids))
                return candidate;

        var created = new FakeArchetype(ids, [.. ids.Select(id => _heapFactories[id]())]);
        _archetypes.Add(created);

        return created;
    }

    // -- the entry points the v1 server SDK binds ----------------------------------------------

    private byte IsEntityAliveImpl(RawEntity entity, byte matchRevision)
    {
        AliveCalls++;
        return Resolve(entity, matchRevision, out _) ? (byte)1 : (byte)0;
    }

    private unsafe void GetComponentSlotImpl(RawEntity entity, byte matchRevision, int componentType, ComponentSlot* slot)
    {
        SlotCalls++;
        *slot = default;

        if (!Resolve(entity, matchRevision, out var found))
            return;

        var heap = found.Archetype.HeapOrNull(componentType);

        if (heap is not null)
            *slot = heap.SlotAt(found.Row);
    }

    private unsafe void QueryImpl(int* componentIds, int n, ChunkCallback callback)
    {
        QueryCalls++;

        var wanted = new int[n];

        for (var i = 0; i < n; i++)
            wanted[i] = componentIds[i];

        var comps = stackalloc ChunkComponent[n];

        foreach (var archetype in _archetypes)
        {
            if (archetype.Count == 0 || !archetype.Fill(wanted, comps))
                continue;

            fixed (RawEntity* entities = archetype.Entities)
                callback((IntPtr)entities, comps, n, archetype.Count);
        }
    }

    private int GetComponentIdByNameImpl(NativeString256 typeName)
        => _idsByName.TryGetValue(typeName.ToManaged(), out var id) ? id : -1;

    private bool Resolve(RawEntity entity, byte matchRevision, out EntitySlot slot)
    {
        slot = default;

        if (entity.Id <= 0 || entity.Id >= _entities.Count)
            return false;

        var candidate = _entities[entity.Id];

        if (!candidate.Alive || (matchRevision != 0 && candidate.Revision != entity.Revision))
            return false;

        slot = candidate;
        return true;
    }

    private readonly record struct EntitySlot
    {
        internal FakeArchetype Archetype { get; init; }

        internal int Row { get; init; }

        internal short Revision { get; init; }

        internal bool Alive { get; init; }
    }
}
