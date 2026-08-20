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

`--recursive` matters. Two submodules under `src/`:

- `Friflo.Engine.ECS`, our fork of [Friflo.Engine.ECS](https://github.com/friflo/Friflo.Engine.ECS)
- `LiteNetLib`, upstream [LiteNetLib](https://github.com/RevenantX/LiteNetLib) pinned to a release commit

`src/` has its own `Directory.Build.props` and does not inherit from anything above it, so the
projects build the same standalone as they do inside a game SDK checkout.
