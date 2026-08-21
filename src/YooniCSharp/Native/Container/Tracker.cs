using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace Yooni.Native.Container;

[StructLayout(LayoutKind.Sequential)]
public struct Tracker
{
    private int _index;
    private NativeTrackHelper.TrackEntry _entry;

    // ReSharper disable once ConvertToAutoProperty
    public int Index
        => _index;

    public int AllocVersion
        => _entry.AllocVersion;

    public int ChangeCount
        => _entry.ChangeCount;

    public static Tracker Alloc()
    {
        Tracker result;
        result._index = NativeTrackHelper.Instance.TrackAlloc(out result._entry);
        return result;
    }

    public void Free()
        => NativeTrackHelper.Instance.TrackFree(_index, ref _entry);

    public readonly void Check()
        => NativeTrackHelper.Instance.Check(_index, in _entry);

    public void MarkChange()
        => NativeTrackHelper.Instance.MarkChange(_index, ref _entry);

    public void MarkChangeNoCheck()
        => NativeTrackHelper.Instance.MarkChangeNoCheck(_index, ref _entry);
}
