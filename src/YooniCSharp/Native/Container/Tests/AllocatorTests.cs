using NUnit.Framework;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container.Tests;

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