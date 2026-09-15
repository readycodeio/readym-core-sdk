using System.ComponentModel;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using IComponent = Friflo.Engine.ECS.IComponent;

namespace ReadyM.SDK.Entity;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct EntityHandle
{
    private readonly RawEntity _rawEntity;
    private readonly IEntityApi _api;

    public EntityHandle(RawEntity rawEntity, IEntityApi api)
    {
        _rawEntity = rawEntity;
        _api = api;
    }

    /// <summary>
    /// Handle to an entity known only by its id, which is all the server mod host ever has: the
    /// relay world lives in the AOT process and revisions do not cross the interop boundary.
    /// </summary>
    public EntityHandle(int entityId, IEntityApi api)
    {
        _rawEntity = RawEntityBits.FromId(entityId);
        _api = api;
    }

    public int Id => _rawEntity.Id;

    public bool IsAlive() => _api.IsAlive(_rawEntity);

    public bool HasComponent<T>() where T : struct, IComponent
    {
        return _api.HasComponent<T>(_rawEntity);
    }

    public ref T GetComponent<T>() where T : struct, IComponent
    {
        return ref _api.GetComponent<T>(_rawEntity);
    }

    public bool TryGetComponent<T>(out T component) where T : struct, IComponent
    {
        return _api.TryGetComponent(_rawEntity, out component);
    }

    public void AddComponent<T>() where T : struct, IComponent
    {
        _api.AddComponent<T>(_rawEntity);
    }

    public bool HasTag<T>() where T : struct, ITag
    {
        return _api.HasTag<T>(_rawEntity);
    }

    public void SetTag<T>(bool set) where T : struct, ITag
    {
        _api.SetTag<T>(_rawEntity, set);
    }

    /// <summary>
    /// <see cref="RawEntity"/> is <see cref="LayoutKind.Explicit"/> over one long, Id at 0 and
    /// Revision at 4, and its (id, revision) constructor is internal to the engine. Replace this
    /// with that constructor if the fork ever makes it public.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    private struct RawEntityBits
    {
        [FieldOffset(0)]
        public long Value;

        [FieldOffset(0)]
        public RawEntity Entity;

        public static RawEntity FromId(int entityId) => new RawEntityBits { Value = (uint)entityId }.Entity;
    }
}