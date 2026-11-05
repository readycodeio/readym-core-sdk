using ReadyM.Native.Container;

namespace ReadyM.Native.Tests.Container;

public class NativeString256Tests : NativeStringTests<NativeString256>
{
    protected override NativeString256 CreateString(string str, bool isWide)
        => new(str, isWide);
}