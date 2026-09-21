using Friflo.Engine.ECS;

namespace ReadyM.Api.Saves;

public interface ISaveLoadContext
{
    /// <summary>The schema version stamped on the component currently being read.</summary>
    ushort Version { get; }

    /// <summary>Reads and resolves entity reference to the loaded entity (default when empty or unresolved).</summary>
    Entity ReadEntityRef(ISaveReader reader);
}
