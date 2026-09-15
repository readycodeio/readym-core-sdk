using ReadyM.SDK.Archetypes.Attributes;

namespace ReadyM.SDK.Tests.Client.Fixtures;

/// An archetype whose components are a subset of Monster's and Chest's, which is what makes the
/// difference between "is this archetype" and "has these components" observable.
[Archetype]
[Include(typeof(Placement))]
public readonly partial struct Prop;
