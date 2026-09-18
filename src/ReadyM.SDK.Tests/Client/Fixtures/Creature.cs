using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Tests.Client.Fixtures;

/// The archetype Monster includes, which is what gives Monster a conversion up to it.
[Archetype]
[Include(typeof(Health))]
public readonly partial struct Creature;
