// Allocation tracking is compiled out in Release (see TrackedAllocator), so these tests only exist in Debug.
#if DEBUG
using System;
using NUnit.Framework;
using ReadyM.Native.Logging;
using ReadyM.Native.LowLevel;
using ReadyM.Native.Container;

namespace ReadyM.Native.Tests.Container;

/// <summary>
/// A root the repo ADOPTS belongs to whoever allocated it. In the game that is the C++ side, which hands its root
/// over so both runtimes track into one table. Releasing an adopted root must hand it back untouched: its owner
/// goes on tracking into it and frees it itself, later.
/// </summary>
public class NativeTrackerAdoptedRootTests
{
    // Stands in for the C++ owner: a repo that allocated its own root and hands it out.
    private NativeTrackerRepo _owner = null!;

    [SetUp]
    public void SetUp()
    {
        _owner = new NativeTrackerRepo();
        _owner.DoInit(AllocatorKind.Marshal);
        NativeTrackerRepo.Init(_owner.GetNativeBinding(), AllocatorKind.Marshal);
    }

    [TearDown]
    public void TearDown()
    {
        ((IDisposable)_owner).Dispose();
    }

    [Test]
    public void Releasing_an_adopted_root_leaves_it_whole_for_its_owner()
    {
        // Tracked through the adopted root, so the owner's table now holds entry 0.
        var list = new NativeList<int>(4, AllocatorKind.Marshal);
        list.Add(1);

        NativeTrackerRepo.Dispose();

        // The owner's table survived with the borrower's entry still in it: the next allocation is entry 1.
        var index = _owner.TrackAlloc(out var entry, NativeLogLevel.Disabled);
        Assert.That(index, Is.EqualTo(1));
        Assert.That(entry.AllocVersion, Is.EqualTo(1));
        Assert.That(_owner.Check(index, in entry), Is.True);

        // And the borrower tracks nothing any more, so a container made through it keeps working untracked.
        list.Add(2);
        Assert.That(list.Count, Is.EqualTo(2));
        list.Dispose();
    }

    [Test]
    public void An_adopted_root_can_be_adopted_again_once_released()
    {
        NativeTrackerRepo.Dispose();

        // A reloaded generation adopts the same owner's root again, which is a fresh start, not a second init.
        Assert.DoesNotThrow(() => NativeTrackerRepo.Init(_owner.GetNativeBinding(), AllocatorKind.Marshal));

        var list = new NativeList<int>(4, AllocatorKind.Marshal);
        list.Add(1);
        Assert.That(list.Count, Is.EqualTo(1));
        list.Dispose();

        NativeTrackerRepo.Dispose();
    }
}
#endif
