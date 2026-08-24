using System.Collections.Generic;
using NUnit.Framework;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container.Tests;

public class NativeDictionaryTests
{
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

    [Test, Category("Native"), Category("NativeDictionary")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestAdd(int initialCapacity)
    {
        var dictionary = new NativeDictionary<int, float, IntHash>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(dictionary.IsCreated);
        Assert.AreEqual(0, dictionary.Count);

        var isAdded = dictionary.Add(123, 0.123f);
        Assert.AreEqual(true, isAdded);
        isAdded = dictionary.Add(new KeyValuePair<int, float>(234, 0.234f));
        Assert.AreEqual(true, isAdded);
        isAdded = dictionary.Add(345, 0.345f);
        Assert.AreEqual(true, isAdded);
        isAdded = dictionary.Add(123, 0.123f);
        Assert.AreEqual(false, isAdded);

        Assert.AreEqual(3, dictionary.Count);

        Assert.AreEqual(0.123f, dictionary[123]);
        Assert.AreEqual(0.234f, dictionary[234]);
        Assert.AreEqual(0.345f, dictionary[345]);

        dictionary.Dispose();
    }


    [Test, Category("Native"), Category("NativeDictionary")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestSet(int initialCapacity)
    {
        var dictionary = new NativeDictionary<int, float, IntHash>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(dictionary.IsCreated);
        Assert.AreEqual(0, dictionary.Count);

        dictionary[123] = 0.123f;
        Assert.AreEqual(0.123f, dictionary[123]);

        dictionary[123] = 0.234f;
        Assert.AreEqual(0.234f, dictionary[123]);

        dictionary.Dispose();
    }

    [Test, Category("Native"), Category("NativeDictionary")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestClear(int initialCapacity)
    {
        var dictionary = new NativeDictionary<int, float, IntHash>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(dictionary.IsCreated);
        Assert.AreEqual(0, dictionary.Count);

        dictionary.Add(123, 0.123f);
        dictionary.Add(234, 0.234f);
        Assert.AreEqual(2, dictionary.Count);

        dictionary.Clear();
        Assert.AreEqual(0, dictionary.Count);

        dictionary.Dispose();
    }

    [Test, Category("Native"), Category("NativeDictionary")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestContains(int initialCapacity)
    {
        var dictionary = new NativeDictionary<int, float, IntHash>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(dictionary.IsCreated);
        Assert.AreEqual(0, dictionary.Count);

        dictionary.Add(123, 0.123f);
        dictionary.Add(234, 0.234f);
        Assert.AreEqual(2, dictionary.Count);

        var result = dictionary.Contains(123, 0.123f);
        Assert.IsTrue(result);
        result = dictionary.Contains(new KeyValuePair<int, float>(123, 0.123f));
        Assert.IsTrue(result);

        result = dictionary.Contains(234, 0.123f);
        Assert.IsFalse(result);
        result = dictionary.Contains(new KeyValuePair<int, float>(234, 0.123f));
        Assert.IsFalse(result);

        result = dictionary.Contains(123, 0.234f);
        Assert.IsFalse(result);
        result = dictionary.Contains(new KeyValuePair<int, float>(123, 0.234f));
        Assert.IsFalse(result);

        dictionary.Dispose();
    }

    [Test, Category("Native"), Category("NativeDictionary")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestContainsKey(int initialCapacity)
    {
        var dictionary = new NativeDictionary<int, float, IntHash>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(dictionary.IsCreated);
        Assert.AreEqual(0, dictionary.Count);

        dictionary.Add(123, 0.123f);
        dictionary.Add(234, 0.234f);
        Assert.AreEqual(2, dictionary.Count);

        bool result;
        result = dictionary.ContainsKey(123);
        Assert.IsTrue(result);

        result = dictionary.ContainsKey(345);
        Assert.IsFalse(result);

        dictionary.Dispose();
    }

    [Test, Category("Native"), Category("NativeDictionary")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestRemove(int initialCapacity)
    {
        var dictionary = new NativeDictionary<int, float, IntHash>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(dictionary.IsCreated);
        Assert.AreEqual(0, dictionary.Count);

        dictionary.Add(123, 0.123f);
        dictionary.Add(234, 0.234f);
        dictionary.Add(345, 0.345f);
        Assert.AreEqual(3, dictionary.Count);

        dictionary.Remove(234);
        Assert.AreEqual(2, dictionary.Count);
        Assert.AreEqual(0.123f, dictionary[123]);
        Assert.AreEqual(0.345f, dictionary[345]);
        dictionary.Remove(345);
        Assert.AreEqual(1, dictionary.Count);
        Assert.AreEqual(0.123f, dictionary[123]);
        dictionary.Remove(123);
        Assert.AreEqual(0, dictionary.Count);

        dictionary.Dispose();
    }

    [Test, Category("Native"), Category("NativeDictionary")]
    [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(10), TestCase(123)]
    public void TestTryGetValue(int initialCapacity)
    {
        var dictionary = new NativeDictionary<int, float, IntHash>(initialCapacity, LowLevel.AllocatorKind.Marshal);
        Assert.IsTrue(dictionary.IsCreated);
        Assert.AreEqual(0, dictionary.Count);

        dictionary.Add(123, 0.123f);
        dictionary.Add(234, 0.234f);
        dictionary.Add(345, 0.345f);

        float value;
        bool result = dictionary.TryGetValue(123, out value);
        Assert.IsTrue(result);
        Assert.AreEqual(value, 0.123f);

        result = dictionary.TryGetValue(1, out value);
        Assert.IsFalse(result);
        Assert.AreEqual(value, 0.0f);

        dictionary.Dispose();
    }
}
