using NUnit.Framework;

namespace Yooni.Native.Container.Tests;

public class NativeListTests
{
    [Test, Category("Native"), Category("NativeList")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestAdd(int initialCapacity)
    {
        var lst = new NativeList<int>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(lst.IsCreated);

        Assert.AreEqual(0, lst.Count);

        int x, y, z;

        x = lst.Add(123);
        Assert.AreEqual(0, x);

        y = lst.Add(234);
        Assert.AreEqual(1, y);

        z = lst.Add(345);
        Assert.AreEqual(2, z);

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

        lst.Dispose();
    }

    [Test, Category("Native"), Category("NativeList")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestInsert(int initialCapacity)
    {
        var lst = new NativeList<int>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(lst.IsCreated);

        Assert.AreEqual(0, lst.Count);

        lst.Insert(0, 123);
        lst.Insert(0, 234);
        lst.Insert(0, 345);

        Assert.AreEqual(3, lst.Count);

        Assert.AreEqual(345, lst[0]);
        Assert.AreEqual(234, lst[1]);
        Assert.AreEqual(123, lst[2]);

        // 345
        // 234 <--
        // 123

        lst.Insert(1, 1230);

        // 345
        // 1230
        // 234
        // 123 <--

        lst.Insert(3, 1240);

        // 345 <--
        // 1230
        // 234
        // 1240
        // 123

        lst.Insert(0, 3450);

        // 3450
        // 345
        // 1230
        // 234
        // 1240
        // 123

        Assert.AreEqual(6, lst.Count);

        Assert.AreEqual(3450, lst[0]);
        Assert.AreEqual(345, lst[1]);
        Assert.AreEqual(1230, lst[2]);
        Assert.AreEqual(234, lst[3]);
        Assert.AreEqual(1240, lst[4]);
        Assert.AreEqual(123, lst[5]);

        CollectionAssert.AreEqual(
            new[] {
                3450, 345,
                1230, 234,
                1240, 123
            },
            lst
        );

        lst.Dispose();
    }

    [Test, Category("Native"), Category("NativeList")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestInsertRange(int initialCapacity)
    {
        var lst = new NativeList<int>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(lst.IsCreated);

        Assert.AreEqual(0, lst.Count);

        lst.InsertRange(0, 123, 2);
        lst.InsertRange(0, 234, 1);

        Assert.AreEqual(3, lst.Count);

        Assert.AreEqual(234, lst[0]);
        Assert.AreEqual(123, lst[1]);
        Assert.AreEqual(123, lst[2]);

        // 234
        // 123 <--
        // 123

        lst.InsertRange(1, 1230, 2);

        // 234
        // 1230
        // 1230
        // 123
        // 123 <--

        lst.InsertRange(4, 1240, 1);

        // 234 <--
        // 1230
        // 1230
        // 123
        // 1240
        // 123

        lst.InsertRange(0, 3450, 2);

        // 3450
        // 3450
        // 234
        // 1230
        // 1230
        // 123
        // 1240
        // 123

        Assert.AreEqual(8, lst.Count);

        Assert.AreEqual(3450, lst[0]);
        Assert.AreEqual(3450, lst[1]);
        Assert.AreEqual(234, lst[2]);
        Assert.AreEqual(1230, lst[3]);
        Assert.AreEqual(1230, lst[4]);
        Assert.AreEqual(123, lst[5]);
        Assert.AreEqual(1240, lst[6]);
        Assert.AreEqual(123, lst[7]);

        CollectionAssert.AreEqual(
            new[] {
                3450, 3450,
                234, 1230,
                1230, 123,
                1240, 123
            },
            lst
        );

        lst.Dispose();
    }

    [Test, Category("Native"), Category("NativeList")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestInsertRangeList(int initialCapacity)
    {
        var lst = new NativeList<int>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(lst.IsCreated);
        Assert.AreEqual(0, lst.Count);

        NativeList<int> source = new NativeList<int>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(source.IsCreated);
        source.Add(234);
        source.Add(123);

        lst.InsertRange(0, source);
        lst.InsertRange(0, 234, 1);

        Assert.AreEqual(3, lst.Count);

        Assert.AreEqual(234, lst[0]);
        Assert.AreEqual(234, lst[1]);
        Assert.AreEqual(123, lst[2]);

        // 234
        // 234 <--
        // 123

        lst.InsertRange(1, source);

        // 234
        // 234
        // 123
        // 234
        // 123 <--

        lst.InsertRange(4, source);

        // 234 <--
        // 234
        // 123
        // 234
        // 234
        // 123
        // 123

        lst.InsertRange(0, source);

        // 234
        // 123
        // 234
        // 234
        // 123
        // 234
        // 234
        // 123
        // 123

        Assert.AreEqual(9, lst.Count);

        Assert.AreEqual(234, lst[0]);
        Assert.AreEqual(123, lst[1]);
        Assert.AreEqual(234, lst[2]);
        Assert.AreEqual(234, lst[3]);
        Assert.AreEqual(123, lst[4]);
        Assert.AreEqual(234, lst[5]);
        Assert.AreEqual(234, lst[6]);
        Assert.AreEqual(123, lst[7]);
        Assert.AreEqual(123, lst[8]);

        CollectionAssert.AreEqual(
            new[] {
                234, 123,
                234, 234,
                123, 234,
                234, 123,
                123
            },
            lst
        );

        lst.Dispose();
    }

    [Test, Category("Native"), Category("NativeList")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestRemoveAt(int initialCapacity)
    {
        var lst = new NativeList<int>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(lst.IsCreated);

        Assert.AreEqual(0, lst.Count);

        lst.Add(123);
        lst.Add(234);
        lst.Add(345);
        lst.Add(456);
        lst.Add(567);

        Assert.AreEqual(5, lst.Count);
        Assert.AreEqual(123, lst[0]);
        Assert.AreEqual(234, lst[1]);
        Assert.AreEqual(345, lst[2]);
        Assert.AreEqual(456, lst[3]);
        Assert.AreEqual(567, lst[4]);

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
        lst.RemoveAt(0);

        Assert.AreEqual(4, lst.Count);
        Assert.AreEqual(234, lst[0]);
        Assert.AreEqual(345, lst[1]);
        Assert.AreEqual(456, lst[2]);
        Assert.AreEqual(567, lst[3]);

        CollectionAssert.AreEqual(
            new[] {
                234, 345,
                456, 567
            },
            lst
        );

        // 234
        // 345
        // 456
        // 567 <--
        lst.RemoveAt(3);

        Assert.AreEqual(3, lst.Count);
        Assert.AreEqual(234, lst[0]);
        Assert.AreEqual(345, lst[1]);
        Assert.AreEqual(456, lst[2]);

        CollectionAssert.AreEqual(
            new[] {
                234, 345,
                456
            },
            lst
        );

        // 234
        // 345 <--
        // 456
        lst.RemoveAt(1);

        Assert.AreEqual(2, lst.Count);
        Assert.AreEqual(234, lst[0]);
        Assert.AreEqual(456, lst[1]);

        CollectionAssert.AreEqual(
            new[] {
                234, 456
            },
            lst
        );

        // 234
        // 456 <--
        lst.RemoveAt(1);

        Assert.AreEqual(1, lst.Count);
        Assert.AreEqual(234, lst[0]);
        CollectionAssert.AreEqual(new[] { 234 }, lst);

        // 234 <--
        lst.RemoveAt(0);

        Assert.AreEqual(0, lst.Count);
        CollectionAssert.AreEqual(new int[] { }, lst);

        lst.Dispose();
    }

    [Test, Category("Native"), Category("NativeList")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestRemoveSwapBack(int initialCapacity)
    {
        var lst = new NativeList<int>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(lst.IsCreated);

        Assert.AreEqual(0, lst.Count);

        lst.Add(123);
        lst.Add(234);
        lst.Add(345);
        lst.Add(456);
        lst.Add(567);

        Assert.AreEqual(5, lst.Count);
        Assert.AreEqual(123, lst[0]);
        Assert.AreEqual(234, lst[1]);
        Assert.AreEqual(345, lst[2]);
        Assert.AreEqual(456, lst[3]);
        Assert.AreEqual(567, lst[4]);

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
        lst.RemoveSwapBack(0);

        Assert.AreEqual(4, lst.Count);
        Assert.AreEqual(567, lst[0]);
        Assert.AreEqual(234, lst[1]);
        Assert.AreEqual(345, lst[2]);
        Assert.AreEqual(456, lst[3]);

        CollectionAssert.AreEqual(
            new[] {
                567, 234,
                345, 456
            },
            lst
        );

        // 567
        // 234
        // 345
        // 456 <--
        lst.RemoveSwapBack(3);

        Assert.AreEqual(3, lst.Count);
        Assert.AreEqual(567, lst[0]);
        Assert.AreEqual(234, lst[1]);
        Assert.AreEqual(345, lst[2]);

        CollectionAssert.AreEqual(
            new[] {
                567, 234,
                345
            },
            lst
        );

        // 567
        // 234 <--
        // 345
        lst.RemoveSwapBack(1);

        Assert.AreEqual(2, lst.Count);
        Assert.AreEqual(567, lst[0]);
        Assert.AreEqual(345, lst[1]);

        CollectionAssert.AreEqual(
            new[] {
                567, 345
            },
            lst
        );

        // 567
        // 345 <--
        lst.RemoveSwapBack(1);

        Assert.AreEqual(1, lst.Count);
        Assert.AreEqual(567, lst[0]);

        CollectionAssert.AreEqual(new[] { 567 }, lst);

        // 567 <--
        lst.RemoveSwapBack(0);

        Assert.AreEqual(0, lst.Count);
        CollectionAssert.AreEqual(new int[] { }, lst);

        lst.Dispose();
    }

    [Test, Category("Native"), Category("NativeList")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestRemoveRange(int initialCapacity)
    {
        var lst = new NativeList<int>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(lst.IsCreated);

        Assert.AreEqual(0, lst.Count);

        lst.Add(123);
        lst.Add(234);
        lst.Add(345);
        lst.Add(456);
        lst.Add(567);
        lst.Add(678);

        Assert.AreEqual(6, lst.Count);
        Assert.AreEqual(123, lst[0]);
        Assert.AreEqual(234, lst[1]);
        Assert.AreEqual(345, lst[2]);
        Assert.AreEqual(456, lst[3]);
        Assert.AreEqual(567, lst[4]);
        Assert.AreEqual(678, lst[5]);

        CollectionAssert.AreEqual(
            new[] {
                123, 234,
                345, 456,
                567, 678
            },
            lst
        );

        // 123 <--
        // 234
        // 345
        // 456
        // 567
        // 678
        lst.RemoveRange(0, 2);

        Assert.AreEqual(4, lst.Count);
        Assert.AreEqual(345, lst[0]);
        Assert.AreEqual(456, lst[1]);
        Assert.AreEqual(567, lst[2]);
        Assert.AreEqual(678, lst[3]);

        CollectionAssert.AreEqual(
            new[] {
                345,
                456,
                567,
                678
            },
            lst
        );

        // 345
        // 456
        // 567
        // 678 <--
        lst.RemoveRange(3, 1);

        Assert.AreEqual(3, lst.Count);
        Assert.AreEqual(345, lst[0]);
        Assert.AreEqual(456, lst[1]);
        Assert.AreEqual(567, lst[2]);

        CollectionAssert.AreEqual(
            new[] {
                345,
                456,
                567
            },
            lst
        );

        // 345
        // 456 <--
        // 567
        lst.RemoveRange(1, 2);

        Assert.AreEqual(1, lst.Count);
        Assert.AreEqual(345, lst[0]);

        CollectionAssert.AreEqual(
            new[] {
                345
            },
            lst
        );

        // 345 <--
        lst.RemoveRange(0 , 1);

        Assert.AreEqual(0, lst.Count);
        CollectionAssert.AreEqual(new int[] { }, lst);

        lst.Dispose();
    }

    [Test, Category("Native"), Category("NativeList")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestIntClear(int initialCapacity)
    {
        var lst = new NativeList<int>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(lst.IsCreated);

        Assert.AreEqual(0, lst.Count);

        lst.Add(123);
        lst.Add(234);
        lst.Add(345);

        Assert.AreEqual(3, lst.Count);

        lst.Clear();

        Assert.AreEqual(0, lst.Count);
        CollectionAssert.AreEqual(new int[] { }, lst);

        lst.Add(111);
        lst.Add(222);
        lst.Add(333);
        lst.Add(444);
        lst.Add(555);
        lst.Add(666);

        Assert.AreEqual(6, lst.Count);
        Assert.AreEqual(111, lst[0]);
        Assert.AreEqual(222, lst[1]);
        Assert.AreEqual(333, lst[2]);
        Assert.AreEqual(444, lst[3]);
        Assert.AreEqual(555, lst[4]);
        Assert.AreEqual(666, lst[5]);

        CollectionAssert.AreEqual(
            new[] {
                111, 222,
                333, 444,
                555, 666
            },
            lst
        );

        lst.Clear();

        Assert.AreEqual(0, lst.Count);
        CollectionAssert.AreEqual(new int[] { }, lst);

        lst.Dispose();
    }

    [Test, Category("Native"), Category("NativeList")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestEnsureLength(int initialCapacity)
    {
        var lst = new NativeList<int>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(lst.IsCreated);

        Assert.AreEqual(0, lst.Count);
        Assert.GreaterOrEqual(lst.Capacity, 0);

        bool resized = lst.EnsureLength(3);

        Assert.IsTrue(resized);
        Assert.AreEqual(3, lst.Count);
        Assert.GreaterOrEqual(lst.Capacity, 3);

        Assert.AreEqual(0, lst[0]);
        Assert.AreEqual(0, lst[1]);
        Assert.AreEqual(0, lst[2]);

        CollectionAssert.AreEqual(
            new[] {
                0, 0,
                0
            },
            lst
        );

        resized = lst.EnsureLength(2);

        Assert.IsFalse(resized);
        Assert.AreEqual(3, lst.Count);
        Assert.GreaterOrEqual(lst.Capacity, 3);

        Assert.AreEqual(0, lst[0]);
        Assert.AreEqual(0, lst[1]);
        Assert.AreEqual(0, lst[2]);

        CollectionAssert.AreEqual(
            new[] {
                0, 0,
                0
            },
            lst
        );

        lst.Clear();

        Assert.AreEqual(0, lst.Count);
        Assert.GreaterOrEqual(lst.Capacity, 0);

        CollectionAssert.AreEqual(new int[] { }, lst);

        resized = lst.EnsureLength(5);

        Assert.IsTrue(resized);
        Assert.AreEqual(5, lst.Count);

        Assert.GreaterOrEqual(lst.Capacity, 5);
        Assert.AreEqual(0, lst[0]);
        Assert.AreEqual(0, lst[1]);
        Assert.AreEqual(0, lst[2]);
        Assert.AreEqual(0, lst[3]);
        Assert.AreEqual(0, lst[4]);

        CollectionAssert.AreEqual(
            new[] {
                0, 0,
                0, 0,
                0
            },
            lst
        );

        resized = lst.EnsureLength(6);

        Assert.IsTrue(resized);
        Assert.AreEqual(6, lst.Count);
        Assert.GreaterOrEqual(lst.Capacity, 6);

        Assert.AreEqual(0, lst[0]);
        Assert.AreEqual(0, lst[1]);
        Assert.AreEqual(0, lst[2]);
        Assert.AreEqual(0, lst[3]);
        Assert.AreEqual(0, lst[4]);
        Assert.AreEqual(0, lst[5]);

        CollectionAssert.AreEqual(
            new[] {
                0, 0,
                0, 0,
                0, 0
            },
            lst
        );

        resized = lst.EnsureLength(0);
        Assert.IsFalse(resized);
        resized = lst.EnsureLength(1);
        Assert.IsFalse(resized);

        Assert.AreEqual(6, lst.Count);
        Assert.GreaterOrEqual(lst.Capacity, 6);

        Assert.AreEqual(0, lst[0]);
        Assert.AreEqual(0, lst[1]);
        Assert.AreEqual(0, lst[2]);
        Assert.AreEqual(0, lst[3]);
        Assert.AreEqual(0, lst[4]);
        Assert.AreEqual(0, lst[5]);

        CollectionAssert.AreEqual(
            new[] {
                0, 0,
                0, 0,
                0, 0
            },
            lst
        );

        resized = lst.EnsureLength(100);

        Assert.IsTrue(resized);
        Assert.AreEqual(100, lst.Count);
        Assert.GreaterOrEqual(lst.Capacity, 100);
        for (var i = 0; i < 100; i++)
        {
            Assert.AreEqual(0, lst[i]);
        }

        lst.Dispose();
    }

    [Test, Category("Native"), Category("NativeList")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestMemClear(int initialCapacity)
    {
        var lst = new NativeList<int>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(lst.IsCreated);

        Assert.AreEqual(0, lst.Count);

        lst.Add(123);
        lst.Add(234);
        lst.Add(345);

        Assert.AreEqual(3, lst.Count);

        lst.ZeroMemory(1, 2);

        Assert.AreEqual(3, lst.Count);
        Assert.AreEqual(123, lst[0]);
        Assert.AreEqual(0, lst[1]);
        Assert.AreEqual(0, lst[2]);

        CollectionAssert.AreEqual(new[] { 123, 0, 0}, lst);

        lst.Add(111);
        lst.Add(222);
        lst.Add(333);

        Assert.AreEqual(6, lst.Count);
        Assert.AreEqual(123, lst[0]);
        Assert.AreEqual(0, lst[1]);
        Assert.AreEqual(0, lst[2]);
        Assert.AreEqual(111, lst[3]);
        Assert.AreEqual(222, lst[4]);
        Assert.AreEqual(333, lst[5]);

        CollectionAssert.AreEqual(
            new[] {
                123, 0,
                0, 111,
                222, 333
            },
            lst
        );

        lst.ZeroMemory(0, 5);

        Assert.AreEqual(6, lst.Count);
        Assert.AreEqual(0, lst[0]);
        Assert.AreEqual(0, lst[1]);
        Assert.AreEqual(0, lst[2]);
        Assert.AreEqual(0, lst[3]);
        Assert.AreEqual(0, lst[4]);
        Assert.AreEqual(333, lst[5]);

        CollectionAssert.AreEqual(
            new[] {
                0, 0,
                0, 0,
                0, 333
            },
            lst
        );

        lst.ZeroMemory(4, 2);

        Assert.AreEqual(6, lst.Count);
        Assert.AreEqual(0, lst[0]);
        Assert.AreEqual(0, lst[1]);
        Assert.AreEqual(0, lst[2]);
        Assert.AreEqual(0, lst[3]);
        Assert.AreEqual(0, lst[4]);
        Assert.AreEqual(0, lst[5]);

        CollectionAssert.AreEqual(
            new[] {
                0, 0,
                0, 0,
                0, 0
            },
            lst
        );

        lst.Dispose();
    }

    [Test, Category("Native"), Category("NativeList")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestResize(int initialCapacity)
    {
        var lst = new NativeList<int>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(lst.IsCreated);

        Assert.AreEqual(0, lst.Count);
        Assert.GreaterOrEqual(lst.Capacity, 0);

        lst.Resize(3);

        Assert.AreEqual(3, lst.Count);
        Assert.GreaterOrEqual(lst.Capacity, 3);
        Assert.AreEqual(0, lst[0]);
        Assert.AreEqual(0, lst[1]);
        Assert.AreEqual(0, lst[2]);

        CollectionAssert.AreEqual(
            new[] {
                0, 0,
                0
            },
            lst
        );

        lst.Resize(2);

        Assert.AreEqual(2, lst.Count);
        Assert.GreaterOrEqual(lst.Capacity, 3);
        Assert.AreEqual(0, lst[0]);
        Assert.AreEqual(0, lst[1]);

        CollectionAssert.AreEqual(
            new[] {
                0, 0
            },
            lst
        );

        lst.Clear();

        Assert.AreEqual(0, lst.Count);
        Assert.GreaterOrEqual(lst.Capacity, 0);

        CollectionAssert.AreEqual(new int[] { }, lst);

        lst.Resize(5);

        Assert.AreEqual(5, lst.Count);
        Assert.GreaterOrEqual(lst.Capacity, 5);
        Assert.AreEqual(0, lst[0]);
        Assert.AreEqual(0, lst[1]);
        Assert.AreEqual(0, lst[2]);
        Assert.AreEqual(0, lst[3]);
        Assert.AreEqual(0, lst[4]);

        CollectionAssert.AreEqual(
            new[] {
                0, 0,
                0, 0,
                0
            },
            lst
        );

        lst.Resize(6);

        Assert.AreEqual(6, lst.Count);
        Assert.GreaterOrEqual(lst.Capacity, 6);
        Assert.AreEqual(0, lst[0]);
        Assert.AreEqual(0, lst[1]);
        Assert.AreEqual(0, lst[2]);
        Assert.AreEqual(0, lst[3]);
        Assert.AreEqual(0, lst[4]);
        Assert.AreEqual(0, lst[5]);

        CollectionAssert.AreEqual(
            new[] {
                0, 0,
                0, 0,
                0, 0
            },
            lst
        );

        lst.Resize(0);
        lst.Resize(1);

        Assert.AreEqual(1, lst.Count);
        Assert.GreaterOrEqual(lst.Capacity, 6);
        Assert.AreEqual(0, lst[0]);

        CollectionAssert.AreEqual(
            new[] {
                0
            },
            lst
        );

        lst.Resize(100);

        Assert.AreEqual(100, lst.Count);
        Assert.GreaterOrEqual(lst.Capacity, 100);
        for (var i = 0; i < 100; i++)
        {
            Assert.AreEqual(0, lst[i]);
        }

        lst.Dispose();
    }
}