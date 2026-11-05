using System.Runtime.CompilerServices;

// V1 SDK. Until mod loading discovers and registers the attributed declarations, the tests wire the
// client services by hand and so need the implementations behind IEntityApi and IEntities.
[assembly: InternalsVisibleTo("ReadyM.SDK.Tests")]
[assembly: InternalsVisibleTo("ReadyM.SDK.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("WukongMp.Sdk")]
[assembly: InternalsVisibleTo("OblivionMpCSharpMod")]
[assembly: InternalsVisibleTo("WukongMp.Api")]

// The relay's test clients register archetypes the way a game does, through ArchetypeExtensionRegistration.
[assembly: InternalsVisibleTo("ReadyM.Relay.Tests")]

// The relay client in Relay/ is internal; the games' client SDKs drive it.
[assembly: InternalsVisibleTo("OblivionMP.SDK")]
[assembly: InternalsVisibleTo("WukongMp.Tests")]
