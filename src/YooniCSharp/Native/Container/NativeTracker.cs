using System.Runtime.InteropServices;

namespace Yooni.Native.Container;

[StructLayout(LayoutKind.Sequential)]
public struct NativeTracker
{
    private int _index;
    private NativeTrackerRepo.TrackEntry _entry;

    // ReSharper disable once ConvertToAutoProperty
    public int Index
        => _index;

    public int AllocVersion
        => _entry.AllocVersion;

    public int ChangeCount
        => _entry.ChangeCount;

    public static NativeTracker Alloc()
    {
        NativeTracker result;
        result._index = NativeTrackerRepo.Instance.TrackAlloc(out result._entry);
        return result;
    }

    public void Free()
        => NativeTrackerRepo.Instance.TrackFree(_index, ref _entry);

    public readonly void Check()
        => NativeTrackerRepo.Instance.Check(_index, in _entry);

    public void MarkChange()
        => NativeTrackerRepo.Instance.MarkChange(_index, ref _entry);

    public void MarkChangeNoCheck()
        => NativeTrackerRepo.Instance.MarkChangeNoCheck(_index, ref _entry);
}
