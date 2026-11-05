using ReadyM.Native.Container;

namespace ReadyM.Native.Tests.Container;

public class NativeString64Tests : NativeStringTests<NativeString64>
{
    protected override NativeString64 CreateString(string str, bool isWide)
        => new(str, isWide);
}