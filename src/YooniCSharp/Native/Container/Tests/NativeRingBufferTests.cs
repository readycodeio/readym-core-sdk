using System;
using NUnit.Framework;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container.Tests;

public class NativeRingBufferTests
{
    [Test, Category("Native"), Category("NativeRingBuffer")]
    public void TestPush()
    {
        var lst = new NativeRingBuffer<int, Storage32<int>>();

        Assert.AreEqual(0, lst.Count);

        Assert.IsTrue(lst.Push(123));
        Assert.AreEqual(1, lst.Count);
        Assert.AreEqual(123, lst[0]);
        Assert.AreEqual(123, lst.Oldest);
        Assert.AreEqual(123, lst.Newest);

        Assert.IsTrue(lst.Push(234));
        Assert.AreEqual(2, lst.Count);
        Assert.AreEqual(123, lst[0]);
        Assert.AreEqual(234, lst[1]);
        Assert.AreEqual(123, lst.Oldest);
        Assert.AreEqual(234, lst.Newest);

        Assert.IsTrue(lst.Push(345));
        Assert.AreEqual(3, lst.Count);
        Assert.AreEqual(123, lst[0]);
        Assert.AreEqual(234, lst[1]);
        Assert.AreEqual(345, lst[2]);
        Assert.AreEqual(123, lst.Oldest);
        Assert.AreEqual(345, lst.Newest);

        CollectionAssert.AreEqual(
            new[] {
                123, 234,
                345
            },
            lst
        );
    }

    [Test, Category("Native"), Category("NativeRingBuffer")]
    public void TestPop()
    {
        var lst = new NativeRingBuffer<int, Storage32<int>>();

        Assert.AreEqual(0, lst.Count);

        lst.Push(123);
        lst.Push(234);
        lst.Push(345);
        lst.Push(456);
        lst.Push(567);

        Assert.AreEqual(5, lst.Count);
        Assert.AreEqual(123, lst[0]);
        Assert.AreEqual(234, lst[1]);
        Assert.AreEqual(345, lst[2]);
        Assert.AreEqual(456, lst[3]);
        Assert.AreEqual(567, lst[4]);
        Assert.AreEqual(123, lst.Oldest);
        Assert.AreEqual(567, lst.Newest);

        CollectionAssert.AreEqual(
            new[] {
                123, 234,
                345, 456,
                567
            },
            lst
        );

        // 123 <--
        // 234
        // 345
        // 456
        // 567
        lst.Pop();

        Assert.AreEqual(4, lst.Count);
        Assert.AreEqual(234, lst[0]);
        Assert.AreEqual(345, lst[1]);
        Assert.AreEqual(456, lst[2]);
        Assert.AreEqual(567, lst[3]);
        Assert.AreEqual(234, lst.Oldest);
        Assert.AreEqual(567, lst.Newest);

        CollectionAssert.AreEqual(
            new[] {
                234, 345,
                456, 567
            },
            lst
        );

        // 234 <--
        // 345
        // 456
        // 567
        lst.Pop();

        Assert.AreEqual(3, lst.Count);
        Assert.AreEqual(345, lst[0]);
        Assert.AreEqual(456, lst[1]);
        Assert.AreEqual(567, lst[2]);
        Assert.AreEqual(345, lst.Oldest);
        Assert.AreEqual(567, lst.Newest);

        CollectionAssert.AreEqual(
            new[] {
                345, 456,
                567
            },
            lst
        );

        // 345 <--
        // 456
        // 567
        lst.Pop();

        Assert.AreEqual(2, lst.Count);
        Assert.AreEqual(456, lst[0]);
        Assert.AreEqual(567, lst[1]);
        Assert.AreEqual(456, lst.Oldest);
        Assert.AreEqual(567, lst.Newest);

        CollectionAssert.AreEqual(
            new[] {
                456, 567
            },
            lst
        );

        // 456 <--
        // 567
        lst.Pop();

        Assert.AreEqual(1, lst.Count);
        Assert.AreEqual(567, lst[0]);
        Assert.AreEqual(567, lst.Oldest);
        Assert.AreEqual(567, lst.Newest);

        CollectionAssert.AreEqual(new[] { 567 }, lst);

        // 567 <--
        lst.Pop();

        Assert.AreEqual(0, lst.Count);
        CollectionAssert.AreEqual(new int[] { }, lst);

        // should be a no-op on empty
        lst.Pop();

        Assert.AreEqual(0, lst.Count);
        CollectionAssert.AreEqual(new int[] { }, lst);
    }

    [Test, Category("Native"), Category("NativeRingBuffer")]
    public void TestClear()
    {
        var lst = new NativeRingBuffer<int, Storage32<int>>();

        Assert.AreEqual(0, lst.Count);

        lst.Push(123);
        lst.Push(234);
        lst.Push(345);

        Assert.AreEqual(3, lst.Count);
        Assert.AreEqual(123, lst[0]);
        Assert.AreEqual(234, lst[1]);
        Assert.AreEqual(345, lst[2]);

        CollectionAssert.AreEqual(
            new[] {
                123, 234,
                345
            },
            lst
        );

        lst.Clear();

        Assert.AreEqual(0, lst.Count);
        CollectionAssert.AreEqual(new int[] { }, lst);

        lst.Push(111);
        lst.Push(222);
        lst.Push(333);
        lst.Push(444);

        Assert.AreEqual(4, lst.Count);
        Assert.AreEqual(111, lst[0]);
        Assert.AreEqual(222, lst[1]);
        Assert.AreEqual(333, lst[2]);
        Assert.AreEqual(444, lst[3]);
        Assert.AreEqual(111, lst.Oldest);
        Assert.AreEqual(444, lst.Newest);

        CollectionAssert.AreEqual(
            new[] {
                111, 222,
                333, 444
            },
            lst
        );

        lst.Clear();

        Assert.AreEqual(0, lst.Count);
        CollectionAssert.AreEqual(new int[] { }, lst);
    }

    [Test, Category("Native"), Category("NativeRingBuffer")]
    public void TestWrapAroundWithoutOverwrite()
    {
        var lst = new NativeRingBuffer<int, Storage32<int>>();

        Assert.AreEqual(0, lst.Count);

        lst.Push(123);
        lst.Push(234);
        lst.Push(345);
        lst.Push(456);
        lst.Push(567);

        Assert.AreEqual(5, lst.Count);

        // remove a couple from the front so head moves forward
        lst.Pop();
        lst.Pop();

        Assert.AreEqual(3, lst.Count);
        Assert.AreEqual(345, lst[0]);
        Assert.AreEqual(456, lst[1]);
        Assert.AreEqual(567, lst[2]);
        Assert.AreEqual(345, lst.Oldest);
        Assert.AreEqual(567, lst.Newest);

        CollectionAssert.AreEqual(
            new[] {
                345, 456,
                567
            },
            lst
        );

        // these should wrap and fill slots at the start of the storage
        lst.Push(678);
        lst.Push(789);

        Assert.AreEqual(5, lst.Count);
        Assert.AreEqual(345, lst[0]);
        Assert.AreEqual(456, lst[1]);
        Assert.AreEqual(567, lst[2]);
        Assert.AreEqual(678, lst[3]);
        Assert.AreEqual(789, lst[4]);
        Assert.AreEqual(345, lst.Oldest);
        Assert.AreEqual(789, lst.Newest);

        CollectionAssert.AreEqual(
            new[] {
                345, 456,
                567, 678,
                789
            },
            lst
        );
    }

    [Test, Category("Native"), Category("NativeRingBuffer")]
    public void TestOverwriteWhenFull()
    {
        var lst = new NativeRingBuffer<int, Storage32<int>>();
        var capacity = lst.Capacity;

        Assert.AreEqual(0, lst.Count);
        Assert.AreEqual(32, capacity);

        for (var i = 0; i < capacity; i++)
        {
            Assert.IsTrue(lst.Push(i + 1));
        }

        Assert.AreEqual(capacity, lst.Count);
        Assert.AreEqual(1, lst.Oldest);
        Assert.AreEqual(capacity, lst.Newest);

        for (var i = 0; i < capacity; i++)
        {
            Assert.AreEqual(i + 1, lst[i]);
        }

        // one extra push should overwrite the oldest and keep count unchanged
        Assert.IsTrue(lst.Push(999));

        Assert.AreEqual(capacity, lst.Count);
        Assert.AreEqual(2, lst.Oldest);
        Assert.AreEqual(999, lst.Newest);

        for (var i = 0; i < capacity - 1; i++)
        {
            Assert.AreEqual(i + 2, lst[i]);
        }
        Assert.AreEqual(999, lst[capacity - 1]);

        // another overwrite
        Assert.IsTrue(lst.Push(1000));

        Assert.AreEqual(capacity, lst.Count);
        Assert.AreEqual(3, lst.Oldest);
        Assert.AreEqual(1000, lst.Newest);

        for (var i = 0; i < capacity - 2; i++)
        {
            Assert.AreEqual(i + 3, lst[i]);
        }
        Assert.AreEqual(999, lst[capacity - 2]);
        Assert.AreEqual(1000, lst[capacity - 1]);
    }

    [Test, Category("Native"), Category("NativeRingBuffer")]
    public void TestIndexerOutOfBounds()
    {
        var lst = new NativeRingBuffer<int, Storage32<int>>();

        Assert.AreEqual(0, lst.Count);

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = lst[0];
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = lst[-1];
        });

        lst.Push(123);
        lst.Push(234);

        Assert.AreEqual(2, lst.Count);
        Assert.AreEqual(123, lst[0]);
        Assert.AreEqual(234, lst[1]);

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = lst[2];
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = lst[-1];
        });
    }

    [Test, Category("Native"), Category("NativeRingBuffer")]
    public void TestOldestAndNewest()
    {
        var lst = new NativeRingBuffer<int, Storage32<int>>();

        lst.Push(123);
        Assert.AreEqual(123, lst.Oldest);
        Assert.AreEqual(123, lst.Newest);

        lst.Push(234);
        Assert.AreEqual(123, lst.Oldest);
        Assert.AreEqual(234, lst.Newest);

        lst.Push(345);
        Assert.AreEqual(123, lst.Oldest);
        Assert.AreEqual(345, lst.Newest);

        lst.Pop();
        Assert.AreEqual(234, lst.Oldest);
        Assert.AreEqual(345, lst.Newest);

        lst.Push(456);
        Assert.AreEqual(234, lst.Oldest);
        Assert.AreEqual(456, lst.Newest);

        CollectionAssert.AreEqual(
            new[] {
                234, 345,
                456
            },
            lst
        );
    }

    [Test, Category("Native"), Category("NativeRingBuffer")]
    public void TestRefAccess()
    {
        var lst = new NativeRingBuffer<int, Storage32<int>>();

        lst.Push(123);
        lst.Push(234);
        lst.Push(345);

        lst[1] = 999;
        Assert.AreEqual(123, lst[0]);
        Assert.AreEqual(999, lst[1]);
        Assert.AreEqual(345, lst[2]);

        lst.Oldest = 111;
        Assert.AreEqual(111, lst[0]);
        Assert.AreEqual(345, lst.Newest);

        lst.Newest = 222;
        Assert.AreEqual(222, lst[2]);

        CollectionAssert.AreEqual(
            new[] {
                111, 999,
                222
            },
            lst
        );
    }
}