using System;
using NUnit.Framework;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container.Tests;

[TestFixture]
public class NativeTrackerNativeListTests
{
    [SetUp]
    public void SetUp()
    {
        NativeTrackHelper.Instance.Init(AllocatorKind.Marshal);
    }

    [TearDown]
    public void TearDown()
    {
        NativeTrackHelper.Instance.Dispose();
    }

    [Test]
    public void TestFreshList()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        Assert.IsTrue(list.IsCreated);
        Assert.AreEqual(0, list.Count);
        Assert.AreEqual(4, list.Capacity);

        list.Dispose();
    }

    [Test]
    public void TestShallowCopyCanReadBeforeMutation()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        list.Add(123);
        list.Add(234);

        var copy = list;

        Assert.AreEqual(2, list.Count);
        Assert.AreEqual(2, copy.Count);

        Assert.AreEqual(123, list[0]);
        Assert.AreEqual(234, list[1]);

        Assert.AreEqual(123, copy[0]);
        Assert.AreEqual(234, copy[1]);

        list.Dispose();
    }

    [Test]
    public void TestMutationInvalidatesShallowCopy()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        list.Add(123);

        var stale = list;

        list.Add(234);

        Assert.AreEqual(2, list.Count);

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = stale.Count;
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = stale[0];
        });

        list.Dispose();
    }

    [Test]
    public void TestMutationThroughCopyInvalidatesOriginal()
    {
        var original = new NativeList<int>(4, AllocatorKind.Marshal);

        original.Add(123);

        var copy = original;

        copy.Add(234);

        Assert.AreEqual(2, copy.Count);

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = original.Count;
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = original[0];
        });

        copy.Dispose();
    }

    [Test]
    public void TestDirectElementMutationDoesNotInvalidateCopy()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        list.Add(123);

        var copy = list;

        list[0] = 456;

        Assert.AreEqual(456, list[0]);
        Assert.AreEqual(456, copy[0]);

        Assert.DoesNotThrow(() =>
        {
            _ = list.Count;
        });

        Assert.DoesNotThrow(() =>
        {
            _ = copy.Count;
        });

        list.Dispose();
    }

    [Test]
    public void TestMutationIntentInvalidatesCopyEvenWhenNothingChanges()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        var stale = list;

        list.Remove(123);

        Assert.AreEqual(0, list.Count);

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = stale.Count;
        });

        list.Dispose();
    }

    [Test]
    public void TestClearEmptyListInvalidatesCopy()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        var stale = list;

        list.Clear();

        Assert.AreEqual(0, list.Count);

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = stale.Count;
        });

        list.Dispose();
    }

    [Test]
    public void TestReallocationInvalidatesCopy()
    {
        var list = new NativeList<int>(1, AllocatorKind.Marshal);

        list.Add(123);

        var stale = list;

        list.Add(234);

        Assert.AreEqual(2, list.Count);
        Assert.AreEqual(123, list[0]);
        Assert.AreEqual(234, list[1]);

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = stale.Count;
        });

        list.Dispose();
    }

    [Test]
    public void TestDisposeInvalidatesShallowCopy()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        list.Add(123);

        var stale = list;

        list.Dispose();

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = stale.Count;
        });

        Assert.DoesNotThrow(() =>
        {
            _ = stale.IsCreated;
        });
    }

    [Test]
    public void TestStaleDisposeDetectsDoubleFree()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        list.Add(123);

        var stale = list;

        list.Dispose();

        Assert.Throws<InvalidOperationException>(() =>
        {
            stale.Dispose();
        });
    }

    [Test]
    public void TestStaleCopyCannotMutate()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        list.Add(123);

        var stale = list;

        list.Add(234);

        Assert.Throws<InvalidOperationException>(() =>
        {
            stale.Add(345);
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            stale.Clear();
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            stale.Remove(123);
        });

        Assert.AreEqual(2, list.Count);
        Assert.AreEqual(123, list[0]);
        Assert.AreEqual(234, list[1]);

        list.Dispose();
    }

    [Test]
    public void TestDefaultListTryCreate()
    {
        NativeList<int> list = default;

        Assert.DoesNotThrow(() =>
        {
            list.TryCreate(AllocatorKind.Marshal);
        });

        Assert.IsTrue(list.IsCreated);
        Assert.AreEqual(0, list.Count);

        list.Dispose();
    }

    [Test]
    public void TestTryCreateExistingListDoesNotInvalidateCopy()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        list.Add(123);

        var copy = list;

        list.TryCreate(AllocatorKind.Marshal);

        Assert.DoesNotThrow(() =>
        {
            _ = copy.Count;
        });

        Assert.AreEqual(1, copy.Count);
        Assert.AreEqual(123, copy[0]);

        list.Dispose();
    }

    [Test]
    public void TestAssignRejectsStaleSource()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        list.Add(123);

        var stale = list;

        list.Add(234);

        Assert.Throws<InvalidOperationException>(() =>
        {
            list.Assign(stale);
        });

        Assert.AreEqual(2, list.Count);
        Assert.AreEqual(123, list[0]);
        Assert.AreEqual(234, list[1]);

        list.Dispose();
    }

    [Test]
    public void TestEqualsRejectsStaleOperand()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        list.Add(123);

        var stale = list;

        list.Add(234);

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = list.Equals(stale);
        });

        list.Dispose();
    }

    [Test]
    public void TestMultipleCopiesOnlyCurrentVersionRemainsValid()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        list.Add(123);

        var stale1 = list;

        list.Add(234);

        var stale2 = list;

        list.Add(345);

        Assert.AreEqual(3, list.Count);

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = stale1.Count;
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = stale2.Count;
        });

        Assert.DoesNotThrow(() =>
        {
            _ = list.Count;
        });

        list.Dispose();
    }

    [Test]
    public void TestCopyMadeAfterMutationIsValid()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        list.Add(123);
        list.Add(234);

        var copy = list;

        Assert.DoesNotThrow(() =>
        {
            _ = copy.Count;
        });

        Assert.AreEqual(2, copy.Count);
        Assert.AreEqual(123, copy[0]);
        Assert.AreEqual(234, copy[1]);

        list.Dispose();
    }

    [Test]
    public void TestStaleFreeDoesNotDamageCurrentOwner()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        list.Add(123);

        var stale = list;

        list.Add(234);

        Assert.Throws<InvalidOperationException>(() =>
        {
            stale.Dispose();
        });

        Assert.DoesNotThrow(() =>
        {
            _ = list.Count;
        });

        Assert.AreEqual(2, list.Count);
        Assert.AreEqual(123, list[0]);
        Assert.AreEqual(234, list[1]);

        list.Dispose();
    }

    delegate void Mutator(ref NativeList<int> list);

    [Test]
    public void TestSeveralMutationKindsInvalidateCopy()
    {
        static void AssertInvalidated(Mutator mutate)
        {
            var list = new NativeList<int>(8, AllocatorKind.Marshal);

            list.Add(10);
            list.Add(20);
            list.Add(30);

            var stale = list;

            mutate(ref list);

            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = stale.Count;
            });

            list.Dispose();
        }

        AssertInvalidated((ref list) => list.Add(40));
        AssertInvalidated((ref list) => list.Insert(1, 40));
        AssertInvalidated((ref list) => list.RemoveAt(1));
        AssertInvalidated((ref list) => list.RemoveSwapBack(1));
        AssertInvalidated((ref list) => list.Remove(20));
        AssertInvalidated((ref list) => list.Clear());
        AssertInvalidated((ref list) => list.Resize(5));
        AssertInvalidated((ref list) => list.EnsureLength(5));
    }
}
