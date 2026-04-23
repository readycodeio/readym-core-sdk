using System.Runtime.InteropServices;

namespace Yooni.Native.LowLevel;

[StructLayout(LayoutKind.Sequential)]
public struct Storage8<T> : IStorage<T>
{
    private T _item0;
    private T _item1;
    private T _item2;
    private T _item3;
    private T _item4;
    private T _item5;
    private T _item6;
    private T _item7;
}