using System;
using System.Runtime.InteropServices;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container;

public class NativeTrackerRepo : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct TrackEntry
    {
        // AllocVersion == 0
        // ChangeCount == -1  | uninitialized or deallocated

        // AllocVersion > 0
        // ChangeCount >= 0   | active

        // AllocVersion < 0   | corrupted

        // ChangeCount < -1   | corrupted

        public int AllocVersion;
        private int _changeCount;

        public int ChangeCount
        {
            get => _changeCount - 1;
            set => _changeCount = value + 1;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EntryList
    {
        public NativeList<TrackEntry> Entries;
        public NativeList<int> FreeList;
    }

    private TypedPtr<EntryList> _ptr;
    private AllocatorKind _allocator;
    private bool _alreadyInit;
    private bool _disposed;

    internal static readonly NativeTrackerRepo Instance = new();

    public static void Init(AllocatorKind allocator)
        => Instance.DoInit(allocator);

    public static void Init(IntPtr trackerPtr, AllocatorKind allocator)
        => Instance.DoInit(trackerPtr, allocator);

    public static void Dispose()
        => ((IDisposable)Instance).Dispose();

    internal void DoInit(AllocatorKind allocator)
    {
        if (_alreadyInit)
            throw new InvalidOperationException("[C#] Tracker already initialized");

        _ptr = TypedPtr<EntryList>.Alloc(allocator);
        _allocator = allocator;
        ref var root = ref _ptr.Get();
        root.Entries = new NativeList<TrackEntry>(1024, allocator);
        root.FreeList = new NativeList<int>(1024, allocator);
        _alreadyInit = true;
        _disposed = false;
    }

    internal void DoInit(IntPtr trackerPtr, AllocatorKind allocator)
    {
        if (_alreadyInit)
            throw new InvalidOperationException("[C#] Tracker already initialized");

        _ptr = new TypedPtr<EntryList>(trackerPtr);
        _allocator = allocator;
        _alreadyInit = true;
        _disposed = false;
    }

    void IDisposable.Dispose()
    {
        ref var root = ref _ptr.Get();
        root.Entries.Dispose();
        root.FreeList.Dispose();
        _ptr.Free(_allocator);
        _allocator = default;
        _alreadyInit = false;
        _disposed = true;
    }

    internal int TrackAlloc(out TrackEntry entry)
    {
        if (!_alreadyInit)
        {
            entry = default;
            return -1; // Untracked because we're not done setting up yet
        }

        ref var root = ref _ptr.Get();
        int index;

        if (root.FreeList.Count > 0)
        {
            index = root.FreeList[root.FreeList.Count - 1];
            root.FreeList.RemoveAt(root.FreeList.Count - 1);
            root.Entries[index].AllocVersion++;
            root.Entries[index].ChangeCount = 0;
        }
        else
        {
            index = root.Entries.Count;
            root.Entries.Add(new TrackEntry { AllocVersion = 1, ChangeCount = 0 });
        }

        entry = root.Entries[index];
        return index;
    }

    internal void TrackFree(int index, ref TrackEntry entry)
    {
        if (!Check(index, in entry))
            return; // This is an untracked entry, nothing to do

        ref var root = ref _ptr.Get();
        root.Entries[index].ChangeCount = -1; // NOTE: Special value to denote freed
        entry.ChangeCount = -1;

        root.FreeList.Add(index);
    }

    // NOTE: Returns whether this is a tracked entry
    internal bool Check(int index, in TrackEntry entry)
    {
        if (_disposed)
            return false;

        if (!_alreadyInit)
            return false; // Untracked because we're not done setting up yet

        if (index == -1)
            return false; // Untracked because we didn't set up an entry to track (e.g. alloc during tracker init)

        if (index < 0)
            throw new InvalidOperationException($"Invalid index {index} for tracking. Index must be non-negative.");

        ref var root = ref _ptr.Get();

        if (index >= root.Entries.Count)
            throw new InvalidOperationException(
                $"[C#] Invalid index {index} for tracking. Current entry count: {root.Entries.Count}");

        if (entry.AllocVersion <= 0)
            throw new InvalidOperationException(
                $"[C#] Invalid tracked entry {index} caller alloc version: {entry.AllocVersion}. " +
                $"Possibly caller's memory got corrupted");

        if (entry.ChangeCount == -1)
            throw new InvalidOperationException(
                $"[C#] Corruption: tracked entry {index} caller's entry is marked as freed. This is " +
                $"a potential use-after-free or use-uninitialized bug. Caller " +
                $"alloc version: {entry.AllocVersion}");

        if (entry.ChangeCount < 0)
            throw new InvalidOperationException(
                $"[C#] THIS SHOULD NOT HAPPEN! tracked entry {index} caller's entry is broken. This is " +
                $"a potential use-uninitialized bug or a memory corruption bug. Caller " +
                $"alloc version: {entry.AllocVersion}, caller change count: {entry.ChangeCount}");

        ref var currentEntry = ref root.Entries[index];

        if (currentEntry.AllocVersion <= 0)
            throw new InvalidOperationException(
                $"[C#] THIS SHOULD NOT HAPPEN! Invalid tracked entry {index} current alloc version: {currentEntry.AllocVersion}. " +
                $"Possibly tracker's memory got corrupted");

        if (currentEntry.AllocVersion != entry.AllocVersion)
            throw new InvalidOperationException(
                $"[C#] Corruption: tracked entry {index} has already been freed (then index was reused) but the " +
                $"caller holds a stale copy. Stale alloc version: {entry.AllocVersion}, " +
                $"stale change count: #{entry.ChangeCount}, current alloc version: {currentEntry.AllocVersion}");

        if (currentEntry.ChangeCount == -1)
            throw new InvalidOperationException(
                $"[C#] Corruption: tracked entry {index} has already been freed but the " +
                $"caller holds a stale copy. Stale alloc version: {entry.AllocVersion} change count #{entry.ChangeCount}");

        if (currentEntry.ChangeCount < -1)
            throw new InvalidOperationException(
                $"[C#] THIS SHOULD NOT HAPPEN! something seems to have overwritten tracked entry {index} current change " +
                $"count with an invalid value. Alloc version: {entry.AllocVersion}, " +
                $"caller change count: #{entry.ChangeCount}, current change count: #{currentEntry.ChangeCount}");

        if (currentEntry.ChangeCount > entry.ChangeCount)
            throw new InvalidOperationException(
                $"[C#] Corruption: tracked entry {index} has been modified but the caller holds a " +
                $"stale copy that didn't see that change. Caller alloc version: {entry.AllocVersion}, " +
                $"stale change count: #{entry.ChangeCount}, current change count: #{currentEntry.ChangeCount}");

        if (currentEntry.ChangeCount < entry.ChangeCount)
            throw new InvalidOperationException(
                $"[C#] THIS SHOULD NOT HAPPEN! current tracked entry {index} has a lower change count than the caller's " +
                $"change count. This could be due to memory corruption, accidental overwrite. " +
                $"Caller alloc version: {entry.AllocVersion}, caller change count: #{entry.ChangeCount}, " +
                $"current change count: #{currentEntry.ChangeCount}");

        // NOTE: Check successful
        return true;
    }

    internal void MarkChange(int index, ref TrackEntry entry)
    {
        if (!Check(index, in entry))
            return;

        ref var root = ref _ptr.Get();
        root.Entries[index].ChangeCount++;
        entry.ChangeCount++;
    }

    internal void MarkChangeNoCheck(int index, ref TrackEntry entry)
    {
        if (_disposed || !_alreadyInit || index == -1)
            return;

        if (index < 0)
            throw new InvalidOperationException($"[C#] Invalid index {index} for tracking. Index must be non-negative.");

        ref var root = ref _ptr.Get();
        root.Entries[index].ChangeCount++;
        entry.ChangeCount++;
    }
}
