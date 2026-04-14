namespace Yooni.Native.Container.Tests;

public class NativeString256Tests : NativeStringTests<NativeString256>
{
    protected override NativeString256 CreateString(string str)
        => new(str);
}