using NUnit.Framework;
using ReadyM.Native.LowLevel;
using ReadyM.Native.Container;

namespace ReadyM.Native.Tests.Container;

public class NativeAllocatorTests
{
    [Test, Category("Native"), Category("NativeFixed")]
    public void TestAllocDefault()
    {
        var ptr = TypedPtr<int>.Alloc(AllocatorKind.Default);
        Assert.That(ptr, Is.Not.EqualTo(TypedPtr<int>.Null));

        ptr.Get() = 123;
        Assert.That(ptr.Get(), Is.EqualTo(123));
        
        ptr.Free(AllocatorKind.Default);
    }
}