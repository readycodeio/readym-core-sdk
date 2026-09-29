using System.Diagnostics.CodeAnalysis;
using ReadyM.Api.Idents;

namespace ReadyM.Api.ECS.Worlds;

/// <summary>
/// Read-only lookup between archetype names and the <see cref="ArchetypeId"/>s the registry assigned to them.
/// </summary>
public interface IArchetypeNames
{
    /// <summary>Resolves a name to its archetype; false when no archetype was registered under it.</summary>
    bool TryGetArchetypeId(string name, out ArchetypeId archetypeId);

    /// <summary>Resolves an archetype to its name; false when the archetype is unknown or was registered unnamed.</summary>
    bool TryGetName(ArchetypeId archetypeId, [NotNullWhen(true)] out string? name);
}
