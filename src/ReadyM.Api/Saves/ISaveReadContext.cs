using Friflo.Engine.ECS;

namespace ReadyM.Api.Saves;

public interface ISaveReadContext
{
    /// <summary>Reads and resolves entity reference to the loaded entity (default when empty or unresolved).</summary>
    Entity ReadEntityRef(ISaveReader reader);
}
