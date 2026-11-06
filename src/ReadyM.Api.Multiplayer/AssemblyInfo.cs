using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ReadyM.Relay.Server")]
[assembly: InternalsVisibleTo("ReadyM.Relay.Web")]
[assembly: InternalsVisibleTo("ReadyM.Relay.Server.Sdk")]
[assembly: InternalsVisibleTo("ReadyM.SDK")]
[assembly: InternalsVisibleTo("ReadyM.SDK.Client")]
[assembly: InternalsVisibleTo("ReadyM.SDK.Server")]
[assembly: InternalsVisibleTo("ReadyM.SDK.Tests")]
[assembly: InternalsVisibleTo("ReadyM.SDK.TestHarness")]
[assembly: InternalsVisibleTo("ReadyM.SDK.Benchmarks")]
[assembly: InternalsVisibleTo("ReadyM.Api.Generators.Tests")]
[assembly: InternalsVisibleTo("ReadyM.SDK.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("ReadyM.Relay.Server.PluginHost")]
[assembly: InternalsVisibleTo("ReadyM.Relay.Server.Wukong")]
[assembly: InternalsVisibleTo("ReadyM.Relay.Server.Oblivion")]
[assembly: InternalsVisibleTo("ReadyM.Relay.Tests")]
[assembly: InternalsVisibleTo("ReadyM.Wukong.Common")]
[assembly: InternalsVisibleTo("WukongMp.Api")]
[assembly: InternalsVisibleTo("OblivionMpCSharpMod")]
[assembly: InternalsVisibleTo("WukongMp.Sdk")]
[assembly: InternalsVisibleTo("OblivionMP.SDK")]
[assembly: InternalsVisibleTo("OblivionMP.SDK.Common")]
[assembly: InternalsVisibleTo("ReadyM.Relay.Common.Oblivion")] // TODO: Change name to ReadyM.Oblivion.Common

// A headless stand-in for a game client, for load testing a relay: it builds the connection options a game sends.
[assembly: InternalsVisibleTo("ReadyM.FakeClient")]