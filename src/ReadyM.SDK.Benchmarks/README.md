# ReadyM.SDK.Benchmarks

What each layer over the ECS costs, measured with BenchmarkDotNet.

```bash
dotnet run -c Release --project ReadyM.SDK.Benchmarks -- --filter '*'
```

`--filter` takes a glob, so `--filter '*Regen*'` runs one class. Drop `[ShortRunJob]` from
`BenchmarkWorld`, or pass `--job medium`, when a number looks close enough to matter.

One class per question, each with its own baseline, so no table compares loops that do different
amounts of work:

| Class | Question |
|---|---|
| `SingleReadBenchmarks` | one accessor per entity: the per-entity cost of each layer |
| `RegenBodyBenchmarks` | five accessors on one component: cost per access rather than per entity |
| `FiveComponentBenchmarks` | one accessor on each of five components: what a multi-mixin archetype costs |
| `QueryOverheadBenchmarks` | what a loop pays before its body runs once |

`V0Creep` is a hand-written stand-in for a v0 entity wrapper. The real ones live in the game SDKs,
not in the core, so it reproduces the shape rather than being the real thing.
