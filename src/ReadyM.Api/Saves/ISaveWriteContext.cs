using System.ComponentModel;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Saves;

/// <exclude />
[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISaveWriteContext
{
    /// <summary>Writes a reference to another entity as its stable PersistentId; a null or non-persisted entity writes empty.</summary>
    void WriteEntityRef(ISaveWriter writer, Entity entity);

    /// <summary>Names dimensions across runs; null where no dimension is saved (metadata, tests).</summary>
    IDimensionSaveNames? DimensionNames { get; }
}
