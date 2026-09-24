# ReadyM Core SDK

Game-agnostic runtime that ReadyM's per-game multiplayer SDKs are built on: an ECS, an RPC
layer with source-generated handlers, the relay client and server SDKs, and native interop
primitives.

Nothing here knows about a specific game. The game-specific parts live in their own
repositories, for example [wukongmp-sdk](https://github.com/readycodeio/wukongmp-sdk).

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

## Layout

| project | tfm | |
|---|---|---|
| `ReadyM.Api` | `netstandard2.0`, `net10.0` | ECS, dependency injection, hosted services |
| `ReadyM.Api.Multiplayer` | `netstandard2.0`, `net10.0` | replication, RPC, serialization, protocol |
| `ReadyM.Api.Generators` | `netstandard2.0` | Roslyn generators for component registration and RPC handlers |
| `ReadyM.Relay.Client` | `netstandard2.0`, `net10.0` | client half of the relay protocol |
| `ReadyM.Relay.Server.Sdk` | `net10.0` | what a server-side mod derives from |
| `Yooni.Native.*` | `netstandard2.0`, `net10.0` | native containers, low-level access, serialization |

`netstandard2.0` is not a stylistic choice: anything a game process loads has to target what
that runtime accepts. Server-side code has no such constraint and targets `net10.0`.

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

### System

A `partial class` that is annotated with `[System]` mus declare a `void Update()` or `void Update(Tick)` method.
This method is called once per system/client update loop tick.

// TODO: Rename to `[Service]`

### CreateHandler

Annotated method defined logic that runs immediately after a component of a given shape is created.
This only applies to creation via the SDK - not for entities received through replication.

// TODO: Allow defining both

An "external" form of `[CreateHandler(typeof(Shape))]` is also supported on a partial class defining a `[Service]`.

### DeleteHandler

Analogous to CreateHandler, but runs immediately before a component of a given shape is deleted.