using System.Runtime.CompilerServices;

// V1 SDK. Until mod loading discovers and registers the attributed declarations, the tests wire the
// client services by hand and so need the implementations behind IEntityApi and IEntities.
[assembly: InternalsVisibleTo("ReadyM.SDK.Tests")]
[assembly: InternalsVisibleTo("ReadyM.SDK.Benchmarks")]
[assembly: InternalsVisibleTo("WukongMp.SDK")]
