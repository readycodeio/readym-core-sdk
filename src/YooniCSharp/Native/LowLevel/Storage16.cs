using System.Runtime.InteropServices;

namespace Yooni.Native.LowLevel;

[StructLayout(LayoutKind.Sequential)]
public struct Storage16<T> : IStorage<T>
{
    private T _item0;
    private T _item1;
    private T _item2;
    private T _item3;
    private T _item4;
    private T _item5;
    private T _item6;
    private T _item7;
    private T _item8;
    private T _item9;
    private T _item10;
    private T _item11;
    private T _item12;
    private T _item13;
    private T _item14;
    private T _item15;
}