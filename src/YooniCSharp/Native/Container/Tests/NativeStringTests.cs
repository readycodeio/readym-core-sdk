using System;
using NUnit.Framework;
using CsCheck;

namespace Yooni.Native.Container.Tests;

public abstract class NativeStringTests<TString>
    where TString : unmanaged, INativeString, IEquatable<TString>
{
    protected abstract TString CreateString(string str, bool isWide);
    
    [Test, Category("String"), Category("ECS")]
    [TestCase(false)]
    [TestCase(true)]
    public void TestStringCompare(bool isWide)
    {
        var x = CreateString("abc", isWide);

        Assert.AreEqual(CreateString("abc", isWide), x);
        Assert.AreNotEqual(CreateString("def", isWide), x);
        Assert.AreNotEqual(CreateString("abcc", isWide), x);

        Assert.IsTrue(x.Equals("abc"));
        Assert.IsFalse(x.Equals("def"));
        Assert.IsFalse(x.Equals("abcc"));

        var y = CreateString("aaaaaaaaaaaaaaaa", isWide);
        var z = CreateString("aaaaaaaaaaaaaaaa", isWide);
        var w = CreateString("aaaaaaaaaaaaaaab", isWide);

        Assert.IsTrue(y.Equals(z));
        Assert.IsTrue(!y.Equals(w));
        Assert.IsFalse(y.Equals(w));
        Assert.AreEqual(y, z);
        Assert.AreNotEqual(y, w);
    }

    [Test, Category("String"), Category("ECS")]
    [TestCase(false)]
    [TestCase(true)]
    public void TestToString(bool isWide)
    {
        var x = CreateString("test", isWide);

        var y = x;

        Assert.AreEqual("test", y.ToString());
    }

    [Test, Category("String"), Category("ECS")]
    [TestCase(false)]
    [TestCase(true)]
    public void TestToManaged(bool isWide)
    {
        var x = CreateString("test", isWide);

        Assert.AreEqual("test", x.ToManaged());
    }

    [Test, Category("String"), Category("ECS")]
    [TestCase(false)]
    [TestCase(true)]
    public void TestGetHashCoded(bool isWide)
    {
        var x = CreateString("test", isWide);
        var y = CreateString("test", isWide);
        var hashX = x.GetHashCode();
        var hashY = y.GetHashCode();
        Assert.AreEqual(hashX, hashY);
    }

    [Test]
    public void PropertyEqualityTest()
    {
        Gen.String
            .Where(x => x != null)
            .Sample(str =>
            {
                if (str.Length <= default(TString).Capacity)
                {
                    var u = CreateString(str, false);
                    Assert.AreEqual(str, u.ToManaged());
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => CreateString(str, false));
                }
            });
    }
}