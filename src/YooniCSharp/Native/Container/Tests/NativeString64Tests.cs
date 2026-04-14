namespace Yooni.Native.Container.Tests;

public class NativeString64Tests : NativeStringTests<NativeString64>
{
    protected override NativeString64 CreateString(string str)
        => new(str);
}