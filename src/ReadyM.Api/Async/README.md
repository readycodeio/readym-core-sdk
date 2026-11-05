# ReadyM.Api.Async

A small, low-allocation module for running `async`/`await` code on a single owner thread, advanced
cooperatively by pumping. It is thread-agnostic: continuations run on whatever thread calls `Pump()`, and an
application may create several independent contexts for different purposes. It is a candidate to replace the
mod's Nito.AsyncEx dependency (which allocates heavily).

## Why

.NET resumes an `await` continuation on the captured `SynchronizationContext`, or on the thread pool if none
is captured. When you need continuations to run on a specific thread you own, you install a single-threaded
`SynchronizationContext` that queues continuations and only runs them when you pump it. Then the code an
author writes between `await`s runs on your thread, not the pool.

## Usage

```
var ctx = new PumpedSynchronizationContext(logger);

// On the owner thread, START the async method under the context:
Task task = null!;
ctx.Start(() => task = RunAsync(ct));

// Each iteration of your loop, on the owner thread, drain the work queued so far:
ctx.Pump();
// ... when task.IsCompleted, read task.Result / observe its exception.

async Task RunAsync(CancellationToken ct)
{
    DoWork();
    await ctx.Yield(ct);   // yields until the next pump; resumes on the owner thread
    DoMoreWork();
}
```

`Pump()` is non-blocking: it drains only the continuations queued as of entry and returns, so work a
continuation queues waits for the next pump (it cannot starve the caller within one iteration). Cancelling the
token makes a suspended `Yield(ct)` throw `OperationCanceledException` on the pump thread, so `finally`/`using`
run there.

## Grounded in

Single-threaded SynchronizationContexts are famously easy to get wrong, so this is modelled on well-documented
references:

- Stephen Toub, "Await, SynchronizationContext, and Console Apps" (the canonical single-threaded pump). The
  difference here is that `Pump()` is non-blocking rather than a `RunOnCurrentThread` loop, because the host
  owns the loop.
- Microsoft.VisualStudio.Threading's `SingleThreadedSynchronizationContext` (production single-thread pumping).
- The `System.Threading.SynchronizationContext` documentation.

## Notes

- Few dependencies: BCL plus `Microsoft.Extensions.Logging.Abstractions` (a required `ILogger`, per the
  project logging rules).
- A continuation that throws is logged (`LogError`) and dropped so it cannot kill the pump; it is never
  swallowed silently.
- Single pumper: pump from exactly one thread. Continuations are guaranteed to run on whichever thread calls
  `Start`/`Pump`.
