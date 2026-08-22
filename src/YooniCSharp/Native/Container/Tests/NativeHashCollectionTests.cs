using NUnit.Framework;
using Yooni.Native.Logging;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container.Tests;

public class NativeHashCollectionTests
{
    [Test, Category("Native"), Category("NativeDictionary")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestInitialize(int initialCapacity)
    {
        var collection = new NativeHashCollection<int, float>(initialCapacity, AllocatorKind.Marshal, NativeLogLevel.Disabled);
        Assert.AreEqual(0, collection.Count);
        Assert.GreaterOrEqual(collection.Capacity, 0);

        collection.Dispose();
    }

    [Test, Category("Native"), Category("NativeDictionary")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestInsert(int initialCapacity)
    {
        var collection = new NativeHashCollection<int, float>(initialCapacity, AllocatorKind.Marshal, NativeLogLevel.Disabled);
        Assert.AreEqual(0, collection.Count);
        var entry = collection.Insert(123, 1, 0.123f);
        Assert.AreEqual(1, collection.Count);
        Assert.AreEqual(1, entry.Get().Hash);
        Assert.IsTrue(entry.Get().Next.IsNull);

        collection.Insert(234, 2, 0.234f);
        collection.Insert(345, 3, 0.345f);
        collection.Insert(456, 1, 0.456f);
        Assert.AreEqual(4, collection.Count);
        Assert.GreaterOrEqual(collection.Capacity, 7);

        collection.Dispose();
    }

    [Test, Category("Native"), Category("NativeDictionary")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestGetKey(int initialCapacity)
    {
        var collection = new NativeHashCollection<int, float>(initialCapacity, AllocatorKind.Marshal, NativeLogLevel.Disabled);
        var entry = collection.Insert(123, 1, 0.123f);
        Assert.AreEqual(123, entry.Get().Key);

        collection.Dispose();
    }

    [Test, Category("Native"), Category("NativeDictionary")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestGetValue(int initialCapacity)
    {
        var collection = new NativeHashCollection<int, float>(initialCapacity, AllocatorKind.Marshal, NativeLogLevel.Disabled);
        var entry = collection.Insert(123, 1, 0.123f);
        Assert.AreEqual(0.123f, entry.Get().Value);

        collection.Dispose();
    }

    [Test, Category("Native"), Category("NativeDictionary")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestRemove(int initialCapacity)
    {
        var collection = new NativeHashCollection<int, float>(initialCapacity, AllocatorKind.Marshal, NativeLogLevel.Disabled);
        Assert.AreEqual(0, collection.Count);

        collection.Insert(1, 1, 1.1f);
        collection.Insert(2, 1, 2.1f);
        collection.Insert(3, 1, 3.1f);
        Assert.AreEqual(collection.Count, 3);

        bool bRemoved = collection.Remove(2, 1);
        Assert.AreEqual(bRemoved, true);
        Assert.AreEqual(collection.Count, 2);

        bRemoved = collection.Remove(2, 1);
        Assert.AreEqual(bRemoved, false);
        Assert.AreEqual(collection.Count, 2);

        collection.Remove(1, 1);
        bRemoved = collection.Remove(3, 1);
        Assert.AreEqual(bRemoved, true);
        Assert.AreEqual(collection.Count, 0);

        collection.Dispose();
    }

    [Test, Category("Native"), Category("NativeDictionary")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestFind(int initialCapacity)
    {
        var collection = new NativeHashCollection<int, float>(initialCapacity, AllocatorKind.Marshal, NativeLogLevel.Disabled);
        Assert.AreEqual(0, collection.Count);
        var entry = collection.Insert(123, 1, 0.123f);
        collection.Insert(234, 2, 0.123f);
        var foundEntry = collection.Find(123, 1);
        Assert.AreEqual(entry ==  foundEntry, true);

        foundEntry = collection.Find(123, 2);
        Assert.AreEqual(foundEntry.IsNull, true);

        entry = collection.Insert(345, 1, 0.123f);
        foundEntry = collection.Find(345, 1);
        Assert.AreEqual(entry == foundEntry, true);

        collection.Dispose();
    }

    [Test, Category("Native"), Category("NativeDictionary")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestClear(int initialCapacity)
    {
        var collection = new NativeHashCollection<int, float>(initialCapacity, AllocatorKind.Marshal, NativeLogLevel.Disabled);
        Assert.AreEqual(0, collection.Count);
        collection.Insert(123, 1, 0.123f);
        collection.Insert(234, 2, 0.123f);
        Assert.AreEqual(2, collection.Count);
        collection.Clear();
        Assert.AreEqual(0, collection.Count);

        collection.Dispose();
    }
}
