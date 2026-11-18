using System.ComponentModel;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Saves;

/// <exclude />
[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISaveWriteContext
{
    /// <summary>Writes a reference to another entity as its stable PersistentId; a null or non-persisted entity writes empty.</summary>
    void WriteEntityRef(ISaveWriter writer, Entity entity);

    /// <summary>
    /// A service the game registered for its save code, such as what names its runtime forms in a save.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">The game registered no service of that type.</exception>
    T GetRequiredService<T>() where T : class;
}
