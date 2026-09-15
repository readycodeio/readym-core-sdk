using ReadyM.Api.Idents;
using ReadyM.SDK.Archetypes.Attributes;

namespace ReadyM.SDK.Tests.Examples.Declarations;

[Archetype]
[IncludeArchetype(typeof(ReadyObject))] // include another archetype: its mixins, tags, and a conversion
[Include(typeof(Vitals))] // include a mixin: its accessors appear directly on MainCharacter
[Include(typeof(Equipment), Optional: true)] // optional mixin: read-only nullable accessors (below)
// [Include(typeof(Transform), Prefix: "Spawn")]     // prefixed include: SpawnPosition, SpawnRotation
public readonly partial struct MainCharacter
{
    public partial PlayerId PlayerId { get; } // accessor declared directly: goes into the default component
}
