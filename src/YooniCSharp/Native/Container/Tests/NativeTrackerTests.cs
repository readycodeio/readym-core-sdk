using System;
using NUnit.Framework;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container.Tests;

public class NativeTrackerTests
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
    public void TestFreshCollection()
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
    public void TestMultipleMutationsKeepCurrentCopyValid()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        list.Add(123);
        list.Add(234);
        list.Add(345);
        list.RemoveAt(1);

        Assert.AreEqual(2, list.Count);
        Assert.AreEqual(123, list[0]);
        Assert.AreEqual(345, list[1]);

        list.Dispose();
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

        list.Clear();

        Assert.AreEqual(0, list.Count);

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = stale.Count;
        });

        list.Dispose();
    }

    [Test]
    public void TestFailedMutationStillInvalidatesCopy()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        list.Add(123);

        var stale = list;

        // Assuming Remove returns without changing anything when absent.
        list.Remove(999);

        Assert.AreEqual(1, list.Count);
        Assert.AreEqual(123, list[0]);

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

        // Forces expansion.
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
            stale.RemoveAt(0);
        });

        Assert.AreEqual(2, list.Count);
        Assert.AreEqual(123, list[0]);
        Assert.AreEqual(234, list[1]);

        list.Dispose();
    }

    [Test]
    public void TestStaleCopyCannotDisposeAfterMutation()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        list.Add(123);

        var stale = list;

        list.Add(234);

        Assert.Throws<InvalidOperationException>(() =>
        {
            stale.Dispose();
        });

        // Failed stale dispose must not damage the current owner.
        Assert.AreEqual(2, list.Count);
        Assert.AreEqual(123, list[0]);
        Assert.AreEqual(234, list[1]);

        list.Dispose();
    }

    [Test]
    public void TestDefaultCollectionTryCreate()
    {
        NativeList<int> list = default;

        list.TryCreate(AllocatorKind.Marshal);

        Assert.IsTrue(list.IsCreated);
        Assert.AreEqual(0, list.Count);

        list.Dispose();
    }

    [Test]
    public void TestTryCreateExistingCollectionDoesNotInvalidateCopy()
    {
        var list = new NativeList<int>(4, AllocatorKind.Marshal);

        list.Add(123);

        var copy = list;

        list.TryCreate(AllocatorKind.Marshal);

        Assert.AreEqual(1, list.Count);
        Assert.AreEqual(1, copy.Count);

        Assert.AreEqual(123, list[0]);
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
    public void TestManyCollectionsExerciseTrackerGrowthAndReuse()
    {
        const int count = 2048;

        var lists = new NativeList<int>[count];

        for (var i = 0; i < count; ++i)
        {
            lists[i] = new NativeList<int>(1, AllocatorKind.Marshal);
            lists[i].Add(i);

            Assert.AreEqual(1, lists[i].Count);
            Assert.AreEqual(i, lists[i][0]);
        }

        for (var i = 0; i < count; ++i)
        {
            lists[i].Dispose();
        }

        for (var i = 0; i < count; ++i)
        {
            lists[i] = new NativeList<int>(1, AllocatorKind.Marshal);
            lists[i].Add(i + 10000);

            Assert.AreEqual(1, lists[i].Count);
            Assert.AreEqual(i + 10000, lists[i][0]);
        }

        for (var i = 0; i < count; ++i)
        {
            lists[i].Dispose();
        }
    }

    [Test]
    public void TestOldCopyRemainsInvalidAfterTrackerSlotReuse()
    {
        var first = new NativeList<int>(4, AllocatorKind.Marshal);

        first.Add(123);

        var stale = first;

        first.Dispose();

        // Very likely reuses the just-freed tracker slot due to the LIFO
        // free list.
        var second = new NativeList<int>(4, AllocatorKind.Marshal);

        second.Add(456);

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = stale.Count;
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            stale.Dispose();
        });

        Assert.AreEqual(1, second.Count);
        Assert.AreEqual(456, second[0]);

        second.Dispose();
    }
}
