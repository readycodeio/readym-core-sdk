using System.ComponentModel;
using Friflo.Engine.ECS;
using IComponent = Friflo.Engine.ECS.IComponent;

namespace ReadyM.SDK;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IEntityApi
{
    public bool HasComponent<T>(RawEntity rawEntity) where T : struct, IComponent;
    public ref T GetComponent<T>(RawEntity rawEntity) where T : struct, IComponent;
    bool TryGetComponent<T>(RawEntity rawEntity, out T component) where T : struct, IComponent;
    void AddComponent<T>(RawEntity entity) where T : struct, IComponent;
    bool IsAlive(RawEntity rawEntity);
    bool HasTag<T>(RawEntity rawEntity) where T : struct, ITag;
    void SetTag<T>(RawEntity rawEntity, bool set) where T : struct, ITag;
}