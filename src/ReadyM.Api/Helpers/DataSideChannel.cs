using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using ReadyM.Api.Interop.Registry;
using ReadyM.Api.Mapping;

namespace ReadyM.Api.Helpers;

public sealed class DataSideChannel
{
    // Per-scope-type cache: resolves TEvent's interop class id (if any) once,
    // and exposes a delegate that pushes/pops/checks against the right dict
    // without further reflection at the call site.
    private static class ScopeInfo<TScope> where TScope : struct, IPropagationScope
    {
        public static readonly PropagationDirection Direction;
        public static readonly bool IsInterop;
        public static readonly int InteropEventId; // valid only if IsInterop
        public static readonly Type EventType;

        static ScopeInfo()
        {
            var scope = default(TScope);
            Direction = scope.Direction;
            EventType = scope.EventType;

            if (typeof(IInteropType).IsAssignableFrom(EventType))
            {
                IsInterop = true;
                // Boxing once here is fine; it's a one-shot per TScope.
                InteropEventId = ((IInteropType)Activator.CreateInstance(EventType)!).GetClassId();
            }
            else
            {
                IsInterop = false;
                InteropEventId = 0;
            }
        }
    }

    private sealed class Entry
    {
        public bool IsSet;
    }

    internal sealed class ThreadEntry
    {
        // Managed-only events: keyed by (direction, Type).
        private readonly Dictionary<(PropagationDirection, Type), Entry> _managedEntries = new();

        // Interop events: keyed by (direction, int). Reachable from both managed
        // and native sides; this is the dict the C++ callbacks see.
        private readonly Dictionary<(PropagationDirection, int), Entry> _interopEntries = new();

        public void PushManaged(PropagationDirection direction, Type eventType)
        {
            var key = (direction, eventType);
            if (!_managedEntries.TryGetValue(key, out var entry))
            {
                entry = new Entry();
                _managedEntries.Add(key, entry);
            }

            if (entry.IsSet)
                throw new InvalidOperationException(
                    $"Data of direction {direction} and event type {eventType} is already set in the side channel.");

            entry.IsSet = true;
        }

        public void PopManaged(PropagationDirection direction, Type eventType)
        {
            var key = (direction, eventType);
            if (!_managedEntries.TryGetValue(key, out var entry) || !entry.IsSet)
                throw new InvalidOperationException(
                    $"Data of direction {direction} and event type {eventType} is not set in the side channel.");

            entry.IsSet = false;
        }

        public bool HasManaged(PropagationDirection direction, Type eventType)
            => _managedEntries.TryGetValue((direction, eventType), out var entry) && entry.IsSet;

        public void PushInterop(PropagationDirection direction, int eventId)
        {
            var key = (direction, eventId);
            if (!_interopEntries.TryGetValue(key, out var entry))
            {
                entry = new Entry();
                _interopEntries.Add(key, entry);
            }

            if (entry.IsSet)
                throw new InvalidOperationException(
                    $"Data of direction {direction} and eventId {eventId} is already set in the side channel.");

            entry.IsSet = true;
        }

        public void PopInterop(PropagationDirection direction, int eventId)
        {
            var key = (direction, eventId);
            if (!_interopEntries.TryGetValue(key, out var entry) || !entry.IsSet)
                throw new InvalidOperationException(
                    $"Data of direction {direction} and eventId {eventId} is not set in the side channel.");

            entry.IsSet = false;
        }

        public bool HasInterop(PropagationDirection direction, int eventId)
            => _interopEntries.TryGetValue((direction, eventId), out var entry) && entry.IsSet;
    }

    internal readonly struct Scope<TScope> : IDisposable
        where TScope : struct, IPropagationScope
    {
        private readonly ThreadEntry _threadEntry;

        internal Scope(ThreadEntry threadEntry)
        {
            _threadEntry = threadEntry;
            if (ScopeInfo<TScope>.IsInterop)
                _threadEntry.PushInterop(ScopeInfo<TScope>.Direction, ScopeInfo<TScope>.InteropEventId);
            else
                _threadEntry.PushManaged(ScopeInfo<TScope>.Direction, ScopeInfo<TScope>.EventType);
        }

        public void Dispose()
        {
            if (ScopeInfo<TScope>.IsInterop)
                _threadEntry.PopInterop(ScopeInfo<TScope>.Direction, ScopeInfo<TScope>.InteropEventId);
            else
                _threadEntry.PopManaged(ScopeInfo<TScope>.Direction, ScopeInfo<TScope>.EventType);
        }
    }

    private readonly ThreadLocal<ThreadEntry> _threadEntries = new(() => new ThreadEntry());

    // ----- Managed API -----

    internal Scope<TScope> PushScope<TScope>() where TScope : struct, IPropagationScope
        => new(_threadEntries.Value!);

    internal bool HasData<TScope>() where TScope : struct, IPropagationScope
    {
        var entry = _threadEntries.Value!;
        return ScopeInfo<TScope>.IsInterop
            ? entry.HasInterop(ScopeInfo<TScope>.Direction, ScopeInfo<TScope>.InteropEventId)
            : entry.HasManaged(ScopeInfo<TScope>.Direction, ScopeInfo<TScope>.EventType);
    }

    // ----- Unmanaged API (C++-facing) -----
    // Only interop events are addressable from here; managed-only events live
    // in a separate keyspace the native side cannot reach.

    internal void PushDataInterop(PropagationDirection direction, int eventId)
        => _threadEntries.Value!.PushInterop(direction, eventId);

    internal void PopDataInterop(PropagationDirection direction, int eventId)
        => _threadEntries.Value!.PopInterop(direction, eventId);

    internal bool HasDataInterop(PropagationDirection direction, int eventId)
        => _threadEntries.Value!.HasInterop(direction, eventId);
}