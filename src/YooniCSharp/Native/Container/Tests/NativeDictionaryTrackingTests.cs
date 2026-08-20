using System;
using NUnit.Framework;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container.Tests;

[TestFixture]
public class NativeDictionaryTrackingTests
{
    private struct IntHash : IHashFunction<int>
    {
        public uint ComputeHash(in int key)
            => unchecked((uint)key * 2654435761u);
    }

    private static NativeDictionary<int, int, IntHash> Create()
        => new(4, AllocatorKind.Marshal);

    [SetUp]
    public void SetUp()
    {
        NativeTrackerRepo.Init(AllocatorKind.Default);
    }

    [TearDown]
    public void TearDown()
    {
        NativeTrackerRepo.Dispose();
    }

    [Test]
    public void DefaultValue_TryCreate_CreatesTrackedDictionary()
    {
        NativeDictionary<int, int, IntHash> dict = default;

        dict.TryCreate(AllocatorKind.Marshal);

        Assert.IsTrue(dict.IsCreated);
        Assert.AreEqual(0, dict.Count);

        dict.Dispose();
    }

    [Test]
    public void Mutation_InvalidatesShallowCopy()
    {
        var dict = Create();

        dict.Add(1, 10);

        var stale = dict;

        dict.Add(2, 20);

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = stale.Count;
        });

        dict.Dispose();
    }

    [Test]
    public void NonStructuralSet_StillInvalidatesCopy()
    {
        var dict = Create();

        dict.Add(1, 10);

        var stale = dict;

        // Existing entry: backing allocation need not change.
        dict.Set(1, 20);

        Assert.AreEqual(20, dict[1]);

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = stale.Count;
        });

        dict.Dispose();
    }

    [Test]
    public void DuplicateAdd_StillInvalidatesCopy()
    {
        var dict = Create();

        Assert.IsTrue(dict.Add(1, 10));

        var stale = dict;

        // No actual content change, but this is intentionally considered
        // an attempted mutation.
        Assert.IsFalse(dict.Add(1, 10));

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = stale.Count;
        });

        dict.Dispose();
    }

    [Test]
    public void GetItemRef_ValueMutation_DoesNotInvalidateCopy()
    {
        var dict = Create();

        dict.Add(1, 10);

        var copy = dict;

        ref int value = ref dict.GetItemRef(1);
        value = 123;

        // This is intentionally NOT a structural tracker change.
        // Both structs reference the same underlying allocation.
        Assert.AreEqual(123, dict[1]);
        Assert.AreEqual(123, copy[1]);

        dict.Dispose();
    }

    [Test]
    public void Equals_RejectsStaleOther_EvenWhenBackingPointerIsSame()
    {
        var dict = Create();

        dict.Add(1, 10);

        var stale = dict;

        // Existing-key Set doesn't require reallocation, therefore stale
        // and dict should still carry the same underlying pointer.
        dict.Set(1, 20);

        // This catches the old:
        //
        // if (pointer == other.pointer)
        //     return true;
        //
        // before other._tracker.Check().
        Assert.Throws<InvalidOperationException>(() =>
        {
            dict.Equals(stale);
        });

        dict.Dispose();
    }

    [Test]
    public void Assign_RejectsStaleOther_EvenWhenBackingPointerIsSame()
    {
        var dict = Create();

        dict.Add(1, 10);

        var stale = dict;

        dict.Set(1, 20);

        // Same backing allocation, stale tracker.
        // Must check stale before reference-equality short circuit.
        Assert.Throws<InvalidOperationException>(() =>
        {
            dict.Assign(stale);
        });

        dict.Dispose();
    }

    [Test]
    public void TryCreate_RejectsStaleCreatedCopy()
    {
        var dict = Create();

        dict.Add(1, 10);

        var stale = dict;

        dict.Set(1, 20);

        // stale._impl still says IsCreated, therefore TryCreate()
        // must validate its stale tracker before returning.
        Assert.Throws<InvalidOperationException>(() =>
        {
            stale.TryCreate(AllocatorKind.Marshal);
        });

        dict.Dispose();
    }

    [Test]
    public void DisposeThroughOneCopy_MakesOtherCopyInvalid()
    {
        var dict = Create();

        dict.Add(1, 10);

        var stale = dict;

        dict.Dispose();

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = stale.Count;
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            stale.Dispose();
        });
    }
}
