# ReadyM Core SDK

Game-agnostic runtime that ReadyM's per-game multiplayer SDKs are built on: an ECS, an RPC
layer with source-generated handlers, the relay client and server SDKs, and native interop
primitives.

Nothing here knows about a specific game. The game-specific parts live in their own
repositories, for example [wukongmp-sdk](https://github.com/readycodeio/wukongmp-sdk).

The `develop` branch is the currently worked-on version.
For a stable release, check the tags.

## Not published on its own

There is no `ReadyM.Core` NuGet package. These assemblies ship inside the per-game SDK
packages, distributed by which side of the wire needs them:

```
ReadyM.SDK.Wukong.Common   ReadyM.Api, ReadyM.Api.Multiplayer, ReadyM.Api.Generators, Yooni.*
ReadyM.SDK.Wukong.Client   ReadyM.Relay.Client
ReadyM.SDK.Wukong.Server   ReadyM.Relay.Server.Sdk
```

That is deliberate. The core SDK has no release cadence of its own, so pairing it with a game
SDK version that never shipped with it is not a state you can reach.

## Build

```bash
git clone --recursive https://github.com/readycodeio/readym-core-sdk.git
dotnet build src/ReadyM.Core.sln
```

`--recursive` matters. One submodule under `src/`:

- `Friflo.Engine.ECS`, our fork of [Friflo.Engine.ECS](https://github.com/friflo/Friflo.Engine.ECS)

[LiteNetLib](https://github.com/RevenantX/LiteNetLib) is a plain NuGet reference, since we carry
no changes to it.

`src/` has its own `Directory.Build.props` and does not inherit from anything above it, so the
projects build the same standalone as they do inside a game SDK checkout.

## SDK attributes and generated code

This is a short overview of the attributes and what they do.

### Archetype / ArchetypeMixin

* `[Archetype]` declares a partial struct an archetype of components.
* `[ArchetypeMixin]` declares a partial struct an archetype mixin, with an underlying ECS component.

Both archetypes and mixins are collectively called "shapes".

Each `public partial` property with `{ get; set; }` results in the following being generated:

* a field on the associated internal component type
* [server] the `get` and `set` accessors of the property, which read and write to the component field
* [client] the `get` and `set` accessors of the property, with the setter throwing (use mapping API instead)

A field of a `NativeList<>` or `NativeDictionary<,>` type is treated as a collection of the underlying type
and must be declared as a `private partial T field { get; }` property.
Accessor methods are generated instead of the standard ones.

A shape with no fields still gets an empty component type generated, so that querying for it is possible.

### Replicated

Annotated shapes are replicated to clients in either a reliable or unreliable manner.

### Propagates

Specifies the ECS-to-Game and Game-to-ECS propagation behavior of the shape.
The sync between the game state and the ECS is defined in terms of the "Push" and "Pull" operations:

* To "push" a value in the ECS (a field of a shape) means to apply it to the game state, e.g. applying other player's positions to their puppet actors.
* To "pull" a value means to read it from the game state, mirroring it in the ECS. Examples: pulling the player's position from the game to the ECS each frame, or pulling the monster's HP to the ECS when it changes.

An "override" is a forceful write of a value to the ECS, which will then block any "pull" operations until the next "push" operation. 

**Example:** overriding player's position, usually pulled from the game to the ECS each frame, to teleport them.

The `Propagates` attribute specifies a policy that dictates which of these operations are allowed and in which context.

* `Propagation.GameToEcs` - all clients can pull, nobody can push. Used for "views" over game state that would mean nothing when pushed to the game, like a flag that indicates if you are in a specific area.
* `Propagation.EcsToGame` - all clients can push, nobody can pull. No use case as of yet.
* `Propagation.Both` - all clients can push and pull. Used for values that make sense to be collaboratively modified by all clients, like a shared score or a shared resource pool.
* `Propagation.ServerAuthoritative` - nobody can pull or override, everyone can push. Used for values that are completely authoritative on the server, like a PvP tournament state or player's money.
* `Propagation.OwnershipBased` - only the owner can pull or override, everyone else can only push. Used for values that are replicated to others by a specific client, like the player's position or the player's inventory.

This attribute applies to replicated shapes only.

The source generator adds an appropriate marker interface to the generated component type.

### Include / IncludeArchetype

Flattens another shape into the current one, so that all of its fields are treated as if they were declared in the current shape.

Used to compose mixins into archetypes, or to build archetypes on top of other archetypes.

Generated:

* the `get` accessors of the included shape are added to the including shape type
* [server] the `set` accessors of the included shape are added to the including shape type
* [server] the native collection access methods

### Extends

Inverse of `[Includes]`. Placed on the mixin to add it to an archetype, usually declared in another assembly.

Generated:

* extension `get` accessors of the extending mixin are added to the extended archetype type
* [server] the extension `set` accessors and setter methods (for setting in a query)
* [server] extensions for the native collection access methods

### Index

The decorated partial property is treated as a key for the shape in index lookup.
All setters or setter methods are generated in such a way that they update the index when the value changes (writing the whole component to Friflo).

### ExplicitComponent

Used internally to skip generating the underlying component type for a shape, when the component is already declared somewhere else.
Using `[Replicates]` and `[Propagates]` on such a shape is forbidden, since the replication is configured internally, and propagation behavior is already defined on the underlying component type via a marker interface.

### ExplicitCollection

Used internally to mark a field of an ExplicitComponent as a native collection type, so that access methods are generated properly.

### Service

A `sealed partial class` annotated with `[Service]` is always registered as a singleton in DI.

Every available lifetime method is duck-typed, optional, and private. A service may declare:

* `void Update()`, called once per client or server update loop tick
* `void Start()`, called once the game is up and every mod is in
* `void Stop()`, called when the DI container is disposed
* `void OnEnabled()`, called just before its update starts ticking
* `void OnDisabled()`, called just after its update stops ticking

A `Time` property is available, with `DeltaTime` (seconds since last update), `Elapsed` (seconds since app start) and `Ticks` (updates so far) fields.

Generated:

* `IUpdatingService` and the `Time` property, when an update was declared, plus the registration that makes the game tick it
* `IStartedService`, when a `Start` or an `OnEnabled` was declared
* `IDisposable`, when a `Stop` was declared
* a `public bool Enabled { get; }` property, on every service
* DI registration call
* the create handler registrations, one per watched shape

### Enabling and disabling services

Inject `IServices` to switch any service off and on again while the game runs, including one from another mod:

```csharp
[Service]
public sealed partial class ArenaControl(IServices services)
{
    private void Pause() => services.Disable<Regeneration>();
    private void Resume() => services.Enable<Regeneration>();
}
```

A disabled service stops updating, and its create and delete handlers stop running with it. Its `Time` stops too, so it reads when it last ran rather than how long it has been off.

A service that was off while entities came and went never heard about them, so re-sync whatever state it keeps in `OnEnabled` rather than assuming it is still current.

`OnEnabled` runs before the first tick after each switch on, including the game's own start.
`OnDisabled` runs after the last tick before each switch off. Neither runs when the container is disposed, which is what `Stop` is for.

Disabling one that is already disabled, or enabling one that is already enabled, logs a warning and does nothing.

`Enabled` is read-only.

### UpdateOrder

Placed on the `Update()` method of a service, it specifies the order in which services are updated each tick.

The default priority is 100. Larger priority = executed **later** in the tick.
`Before` and `After` parameters specify constraints against other services, and take precedence over the priority value.

Services with otherwise equal priority are updated in the alphabetical order of their type names.

### CreateHandler

Annotated method defined logic that runs immediately after a component of a given shape is created.
This only applies to creation via the SDK - not for entities received through replication.

// TODO: Allow defining both

An "external" form of `[CreateHandler(typeof(Shape))]` is also supported on a partial class defining a `[Service]`.

Any `[Archetype]` or `[ArchetypeMixin]` can be watched.

There are no guarantees on the order of execution of multiple handlers for the same shape.

### DeleteHandler

Analogous to CreateHandler, but runs immediately before a component of a given shape is deleted. 
Client-side handlers run for everyone, regardless of ownership.

A delete queued inside a query is held until the loop ends, and the handlers run when it is applied.

### ModEntry

A `sealed partial class` annotated with `[ModEntry]` is the entry point of a mod.

It may accept constructor parameters filled from DI. 

An optional `void Init()` method is called when the mod is loaded, 
after DI registers types from the current assembly, so we can inject config classes or services. It
is the registration phase: the container is still open and the game is not up yet, so anything that
touches the game belongs in a `[Service]`.

A `public string ModDirectory { get; }` property is available, which points to the mod's root directory on disk.

### ModConfig

A `sealed partial class` annotated with `[ModConfig]` is a configuration class for a mod.

A "config.json" file (you can override the name) is automatically loaded from the mod's root directory
and deserialized into the class, which is registered in DI.

### RpcContracts

A `static partial class` annotated with `[RpcContracts]`, declared in a mod's Common project, is
one set of RPCs. Each one is a `static partial void` whose name is the RPC and whose parameters are the
payload, marked with the direction it travels:

```csharp
[RpcContracts]
public static partial class CoopRpcContracts
{
    [ClientToServer] public static partial void ScaleBossHp(int percent);
    [ServerToClient] public static partial void BossHpScaled(int percent);
    [ClientToClients(RelayMode.AreaOfInterestAll)] public static partial void Wave(int kind);
}
```

One name is one wire code, so a request and its response share a name and may carry different payloads.
A name is either routed through the server or relayed between clients, never both: the two are numbered
out of separate code spaces, so mixing them under one name is refused.

Implementing a set is a `partial class` annotated with `[RpcHandlersFor(typeof(TheContracts))]`, on
the client, on the server, or both. It needs no base class and may implement any subset of the stubs:

```csharp
[RpcHandlersFor(typeof(CoopRpcContracts))]
public partial class CoopRpc
{
    partial void OnBossHpScaled(int percent) { }

    partial void OnWave(PlayerId sender, int kind) { }
}
```

What each direction generates:

| Direction | On the client | On the server |
| --- | --- | --- |
| `[ClientToServer]` | `SendX(payload)` | `partial void OnX(RpcContext context, payload)` |
| `[ServerToClient]` | `partial void OnX(payload)` | `SendX(PlayerId recipient, payload)` |
| `[ClientToClients(mode)]` | `SendX(payload)` **and** `partial void OnX(PlayerId sender, payload)` | nothing |

A peer-to-peer RPC never reaches a mod on the server: the relay forwards it by the `RelayMode` the
contract named, and every client that receives it runs its own `OnX`. The sender is passed in because
it is the one thing a receiver cannot work out for itself. Nothing is sent before the relay has given
this client a player id.

`RelayMode` picks who hears it: `AreaOfInterestOthers`, `AreaOfInterestAll`, `GlobalOthers` or
`GlobalAll`. The others on the enum are not part of the public API and are refused.

A class carrying `[RpcHandlersFor]` is registered as a singleton in DI automatically, the way a
`[Service]` is.
