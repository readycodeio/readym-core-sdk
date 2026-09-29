using Friflo.Engine.ECS;

namespace ReadyM.Api.Saves;

public interface ISaveWriteContext
{
    /// <summary>Writes a reference to another entity as its stable PersistentId; a null or non-persisted entity writes empty.</summary>
    void WriteEntityRef(ISaveWriter writer, Entity entity);
}
