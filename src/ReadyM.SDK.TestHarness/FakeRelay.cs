using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Interop;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Ecs.Components;
using ReadyM.Api.Idents;
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
    private readonly SetComponentDelegate _setComponent;
    private readonly FindByIndexDelegate _findByIndex;
    private readonly QueryInScopeDelegate _queryInScope;
    private readonly GetComponentSlotDelegate _getComponentSlot;
    private readonly IsEntityAliveDelegate _isEntityAlive;
    private readonly GetComponentIdByNameDelegate _getComponentIdByName;
    private readonly RegisterModComponentDelegate _registerModComponent;
    private readonly RegisterArchetypeDelegate _registerArchetype;
    private readonly CreateLocalEntityDelegate _createLocalEntity;
    private readonly DeleteNetworkedEntityDelegate _deleteEntity;

    // The v0 api binds every pointer in the bundle at construction, so none may be zero. These
    // stand in for the parts of the relay this harness does not model; calling one says so.
    private readonly CreateNetworkedEntityDelegate _createNetworked = static (_, _, _) => throw NotModelled();
    private readonly CreateNetworkedPlayerEntityDelegate _createNetworkedPlayer = static (_, _, _, _) => throw NotModelled();
    private readonly CreateNetworkedAreaEntityDelegate _createNetworkedArea = static (_, _, _, _) => throw NotModelled();
    private readonly CreateNetworkedCellEntityDelegate _createNetworkedCell = static (_, _, _, _) => throw NotModelled();
    private readonly DeleteEntityTreeDelegate _deleteTree = static (_, _) => throw NotModelled();
    private readonly SetParentDelegate _setParent = static (_, _) => throw NotModelled();
    private readonly GetParentDelegate _getParent = static _ => throw NotModelled();
    private readonly GetChildrenDelegate _getChildren = static (_, _, _) => throw NotModelled();

    private static NotSupportedException NotModelled([CallerMemberName] string member = "")
        => new($"The fake relay does not model {member}.");

    // Keyed by the id itself, because ArchetypeId keeps its byte to itself.
    private readonly Dictionary<ArchetypeId, int[]> _archetypeShapes = [];

    internal unsafe FakeRelay()
    {
        _query = QueryImpl;
        _setComponent = SetComponentImpl;
        _findByIndex = FindByIndexImpl;
        _queryInScope = QueryInScopeImpl;
        _getComponentSlot = GetComponentSlotImpl;
        _isEntityAlive = IsEntityAliveImpl;
        _getComponentIdByName = GetComponentIdByNameImpl;
        _registerModComponent = static (_, _) => -1;
        _registerArchetype = RegisterArchetypeImpl;
        _createLocalEntity = CreateLocalEntityImpl;
        _deleteEntity = DeleteEntityImpl;
    }

    internal int QueryCalls { get; private set; }

    internal int SlotCalls { get; private set; }

    internal int AliveCalls { get; private set; }

    internal int CreateCalls { get; private set; }

    internal int DeleteCalls { get; private set; }

    internal void ResetCounters() => (QueryCalls, SlotCalls, AliveCalls, CreateCalls, DeleteCalls) = (0, 0, 0, 0, 0);

    internal EcsApiPointers Pointers => new()
    {
        Query = Marshal.GetFunctionPointerForDelegate(_query),
        GetComponentSlot = Marshal.GetFunctionPointerForDelegate(_getComponentSlot),
        SetComponent = Marshal.GetFunctionPointerForDelegate(_setComponent),
        FindByIndex = Marshal.GetFunctionPointerForDelegate(_findByIndex),
        QueryInScope = Marshal.GetFunctionPointerForDelegate(_queryInScope),
        IsEntityAlive = Marshal.GetFunctionPointerForDelegate(_isEntityAlive),
        CreateLocalEntity = Marshal.GetFunctionPointerForDelegate(_createLocalEntity),
        DeleteNetworkedEntity = Marshal.GetFunctionPointerForDelegate(_deleteEntity),
        CreateNetworkedEntity = Marshal.GetFunctionPointerForDelegate(_createNetworked),
        CreateNetworkedPlayerEntity = Marshal.GetFunctionPointerForDelegate(_createNetworkedPlayer),
        CreateNetworkedAreaEntity = Marshal.GetFunctionPointerForDelegate(_createNetworkedArea),
        CreateNetworkedCellEntity = Marshal.GetFunctionPointerForDelegate(_createNetworkedCell),
        DeleteEntityTree = Marshal.GetFunctionPointerForDelegate(_deleteTree),
        SetParent = Marshal.GetFunctionPointerForDelegate(_setParent),
        GetParent = Marshal.GetFunctionPointerForDelegate(_getParent),
        GetChildren = Marshal.GetFunctionPointerForDelegate(_getChildren)
    };

    internal ArchetypePointers Archetypes => new()
    {
        RegisterArchetype = Marshal.GetFunctionPointerForDelegate(_registerArchetype),
        ModifyArchetype = IntPtr.Zero
    };

    internal AotPointers Aot => new()
    {
        RegisterModComponent = Marshal.GetFunctionPointerForDelegate(_registerModComponent),
        GetComponentIdByName = Marshal.GetFunctionPointerForDelegate(_getComponentIdByName)
    };

    // -- world building, done by a test rather than across the boundary ------------------------

    /// A component the AOT side owns: blittable, pinned, reachable by address.
    internal void RegisterBlittable<T>() where T : struct => Register<T>(static () => new PinnedHeap<T>());

    /// <summary>
    /// A component the relay owns and indexes, which is the case a mod cannot maintain itself.
    /// </summary>
    internal void RegisterIndexed<T, TValue>()
        where T : struct, IIndexedComponent<TValue>
        where TValue : unmanaged
    {
        Register<T>(static () => new PinnedHeap<T>());
        _indexes[_heapFactories.Count - 1] = new FakeIndex<T, TValue>();
    }

    private readonly Dictionary<int, FakeIndex> _indexes = new();

    /// <summary>Counts what a mod could not do for itself, so a test can tell the paths apart.</summary>
    internal int SetComponentCalls { get; private set; }

    internal int FindByIndexCalls { get; private set; }

    internal int QueryInScopeCalls { get; private set; }

    /// Which scope each entity sits in, which is the link the relay reads from InScopeComponent.
    private readonly Dictionary<int, RawEntity> _scopes = new();

    /// <summary>Puts an entity in a scope, as creating one inside a scope would.</summary>
    internal void PutInScope(RawEntity entity, RawEntity scope) => _scopes[entity.Id] = scope;

    /// <summary>
    /// The entities a scope holds that carry every requested component, found from the links
    /// pointing at the scope rather than by scanning, which is what the relay does.
    /// </summary>
    private unsafe void QueryInScopeImpl(int* componentIds, int n, RawEntity scope, EntityListCallback callback)
    {
        QueryInScopeCalls++;

        var wanted = new int[n];

        for (var i = 0; i < n; i++)
            wanted[i] = componentIds[i];

        var matched = new List<RawEntity>();

        foreach (var (id, holder) in _scopes)
        {
            if (holder != scope || !Resolve(RawEntities.From(id, _entities[id].Revision), 1, out var slot))
                continue;

            if (wanted.All(component => slot.Archetype.HeapOrNull(component) is not null))
                matched.Add(RawEntities.From(id, _entities[id].Revision));
        }

        var entities = matched.ToArray();

        fixed (RawEntity* buffer = entities)
            callback(entities.Length == 0 ? IntPtr.Zero : (IntPtr)buffer, entities.Length);
    }

    /// <summary>
    /// Writes the whole component and moves it in the index, which is what the relay does by
    /// assigning through Friflo rather than through the address a slot carries.
    /// </summary>
    private unsafe byte SetComponentImpl(RawEntity entity, byte matchRevision, int componentType, void* data, int size)
    {
        SetComponentCalls++;

        if (!_indexes.TryGetValue(componentType, out var index))
            return 0;

        if (!Resolve(entity, matchRevision, out var slot))
            return 0;

        var heap = slot.Archetype.HeapOrNull(componentType);

        if (heap is null || size != heap.Stride)
            return 0;

        index.Forget(heap.GetValue(slot.Row), entity);
        heap.SetValue(slot.Row, index.Read((IntPtr)data));
        index.Remember(heap.GetValue(slot.Row), entity);

        return 1;
    }

    private unsafe byte FindByIndexImpl(int componentType, void* value, int size, RawEntity* found)
    {
        FindByIndexCalls++;

        *found = default;

        if (!_indexes.TryGetValue(componentType, out var index) || !index.TryFind((IntPtr)value, out var entity))
            return 0;

        *found = entity;
        return 1;
    }

    /// A component a mod owns: reachable only through its heap handle, so managed fields are allowed.
    internal void RegisterManaged<T>() where T : struct => Register<T>(static () => new ManagedHeap<T>());

    /// <summary>
    /// Registers a component this assembly cannot name, which is any component belonging to another
    /// mod. Reflection is fine here: registration happens once, at setup.
    /// </summary>
    internal void RegisterUnnamed(Type component)
    {
        if (_idsByName.ContainsKey(component.FullName!))
            return;

        var heap = typeof(PinnedHeap<>).MakeGenericType(component);

        _idsByName[component.FullName!] = _heapFactories.Count;
        _heapFactories.Add(() => (FakeHeap)Activator.CreateInstance(heap, nonPublic: true)!);
    }

    private void Register<T>(Func<FakeHeap> factory) where T : struct
    {
        _idsByName[typeof(T).FullName!] = _heapFactories.Count;
        _heapFactories.Add(factory);
    }

    private RawEntity Create(int[] componentIds)
    {
        var archetype = GetOrCreateArchetype(componentIds);
        var row = archetype.AppendRow();
        var entity = RawEntities.From(_entities.Count, 1);

        _entities.Add(new EntitySlot { Archetype = archetype, Row = row, Revision = 1, Alive = true });
        archetype.Entities[row] = entity;

        return entity;
    }

    internal RawEntity Create(params Type[] componentTypes)
        => Create(componentTypes.Select(IdOf).OrderBy(id => id).ToArray());

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

    /// <summary>The v0 server api bound to this relay, for comparing the two generations.</summary>
    internal EcsApi CreateEcsApi()
        => new(Pointers, new ComponentRegistry(Aot, new ModComponentManager(), NullLogger.Instance));

    /// The v1 server api bound to this relay, wired the way the mod host wires it.
    internal ServerEntityApi CreateEntityApi()
        => new(Pointers, Archetypes, new ComponentRegistry(Aot, new ModComponentManager(), NullLogger.Instance));

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

    private ArchetypeId RegisterArchetypeImpl(NativeList<int> componentIds)
    {
        var ids = new int[componentIds.Count];

        for (var i = 0; i < ids.Length; i++)
            ids[i] = componentIds[i];

        Array.Sort(ids);

        foreach (var (id, shape) in _archetypeShapes)
            if (shape.AsSpan().SequenceEqual(ids))
                return id;

        var assigned = new ArchetypeId((byte)(_archetypeShapes.Count + 1));

        _archetypeShapes[assigned] = ids;
        return assigned;
    }

    private RawEntity CreateLocalEntityImpl(ArchetypeId archetype)
    {
        CreateCalls++;
        return Create(_archetypeShapes[archetype]);
    }

    private int DeleteEntityImpl(RawEntity entity, byte matchRevision)
    {
        DeleteCalls++;

        if (!Resolve(entity, matchRevision, out var slot))
            return 0;

        var moved = slot.Archetype.RemoveRow(slot.Row);

        if (moved.Id != 0)
            _entities[moved.Id] = _entities[moved.Id] with { Row = slot.Row };

        _entities[entity.Id] = slot with { Alive = false };
        return 1;
    }

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

            // As the relay does it: one scratch buffer refilled per archetype, pinned only for the
            // call. Anything the callee keeps reading afterwards is reading the next chunk's
            // entities, or unpinned memory. See RawEntityScratch on the relay side.
            var identities = Scratch(archetype.Count);

            archetype.Entities.AsSpan(0, archetype.Count).CopyTo(identities);

            fixed (RawEntity* entities = identities)
                callback((IntPtr)entities, comps, n, archetype.Count);
        }
    }

    [ThreadStatic]
    private static RawEntity[]? _scratch;

    private static RawEntity[] Scratch(int count)
    {
        if (_scratch is null || _scratch.Length < count)
            _scratch = new RawEntity[Math.Max(count, 512)];

        return _scratch;
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
