using System.Runtime.InteropServices;
using Yooni.Native.Logging;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container;

[StructLayout(LayoutKind.Sequential)]
public struct TrackedAllocator
{
    public AllocatorKind Kind;

#if DEBUG
    private int _index;
    private NativeTrackerRepo.TrackEntry _entry;
#endif

    public int Index
#if DEBUG
        => _index;
#else
        => -1;
#endif

    public int AllocVersion
#if DEBUG
        => _entry.AllocVersion;
#else
        => default;
#endif

    public int ChangeCount
#if DEBUG
        => _entry.ChangeCount;
#else
        => default;
#endif

    public TrackedAllocator(AllocatorKind allocatorKind, NativeLogLevel level)
    {
        Kind = allocatorKind;
#if DEBUG
        _index = NativeTrackerRepo.Instance.TrackAlloc(out _entry, level);
#endif
    }

    public void Free()
    {
#if DEBUG
        NativeTrackerRepo.Instance.TrackFree(_index, ref _entry);
#endif
        Kind = default;
    }

    public readonly void Check()
#if DEBUG
        => NativeTrackerRepo.Instance.Check(_index, in _entry);
#else
        {}
#endif

    public void MarkChange()
#if DEBUG
        => NativeTrackerRepo.Instance.MarkChange(_index, ref _entry);
#else
        {}
#endif

    public void MarkChangeNoCheck()
#if DEBUG
        => NativeTrackerRepo.Instance.MarkChangeNoCheck(_index, ref _entry);
#else
        {}
#endif

    public readonly NativeLogLevel GetLogging()
#if DEBUG
        => NativeTrackerRepo.Instance.GetLogging(_index, in _entry);
#else
        => NativeLogLevel.Disabled;
#endif

    public void SetLogging(NativeLogLevel level)
#if DEBUG
        => NativeTrackerRepo.Instance.SetLogging(_index, ref _entry, level);
#else
        {}
#endif
}
