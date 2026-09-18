# ReadyM.SDK.Benchmarks

Five comparisons, three arms each: what the same work costs through raw ECS access, through the v0
shape, and through v1.

```bash
dotnet run -c Release --project ReadyM.SDK.Benchmarks -- --filter '*'
```

`--filter` takes a glob, so `--filter '*Mixins*'` runs one set.

| Class | Question |
|---|---|
| `EntityAccessBenchmarks` | reading one component off one entity you already hold |
| `MixinQueryBenchmarks` | a query over one mixin, reading its one field |
| `ArchetypeQueryBenchmarks` | a query over one archetype, reading five fields from its five mixins |
| `MixinsQueryBenchmarks` | a query over five mixins named at the call site, one field from each |
| `ClientQueryBenchmarks` | the archetype query again, on the client, where there is no boundary |

## Which half each runs on

`EntityAccessBenchmarks` runs against an in-process Friflo store, because that is where Friflo, v0
and v1 are literally comparable: a Friflo `Entity`, a v0-style wrapper over one, and a v1 archetype
struct. It does no iteration, so it measures the per-access cost of an entity reference and nothing
else.

The three query sets run against the fake relay, behind real function pointers. That is the half
whose cost is dominated by crossing the interop boundary, and the only one with queries over more
than two shapes. The arms map onto the same three generations:

- **relay chunks** is the floor. The relay's own world is a Friflo world, so the closest a mod can
  get to raw Friflo is walking the chunks a query hands back.
- **v0** is the develop-branch SDK's `EcsApi.Query`, which is already chunk based: one crossing,
  each component's chunk base resolved once per chunk, then a delegate per entity with the
  components by reference. The arms reproduce that loop rather than calling the real thing, whose
  thread-static callback bookkeeping is not what is being measured. Everything that costs anything
  is the same, and if anything it flatters v0 slightly, since the real one resolves component ids
  per query and this does not.
- **v1** is what a mod writes today.

## Known wrinkle

Every class is `[InProcess]`. BenchmarkDotNet builds its own project per run, and that project
resolves a different `Friflo.Engine.ECS` than the project reference does, so fork-only types go
missing at runtime. Running in the host process uses the assemblies already loaded. Worth fixing
properly; until then, do not remove the attribute.

`V0Creep` is a hand-written stand-in for a v0 entity wrapper. The real ones live in the game SDKs,
not in the core, so it reproduces the shape rather than being the real thing.
