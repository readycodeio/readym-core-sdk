using System;
using NUnit.Framework;
using CsCheck;

namespace Yooni.Native.Container.Tests;

public abstract class NativeStringTests<TString>
    where TString : unmanaged, INativeString, IEquatable<TString>
{
    protected abstract TString CreateString(string str);
    
    [Test, Category("String"), Category("ECS")]
    public void TestStringCompare()
    {
        var x = CreateString("abc");

        Assert.AreEqual(CreateString("abc"), x);
        Assert.AreNotEqual(CreateString("def"), x);
        Assert.AreNotEqual(CreateString("abcc"), x);

        Assert.IsTrue(x.Equals("abc"));
        Assert.IsFalse(x.Equals("def"));
        Assert.IsFalse(x.Equals("abcc"));

        var y = CreateString("aaaaaaaaaaaaaaaa");
        var z = CreateString("aaaaaaaaaaaaaaaa");
        var w = CreateString("aaaaaaaaaaaaaaab");

        Assert.IsTrue(y.Equals(z));
        Assert.IsTrue(!y.Equals(w));
        Assert.IsFalse(y.Equals(w));
        Assert.AreEqual(y, z);
        Assert.AreNotEqual(y, w);
    }

    [Test, Category("String"), Category("ECS")]
    public void TestToString()
    {
        var x = CreateString("test");

        var y = x;

        Assert.AreEqual("test", y.ToString());
    }

    [Test, Category("String"), Category("ECS")]
    public void TestToManaged()
    {
        var x = CreateString("test");

        Assert.AreEqual("test", x.ToManaged());
    }

    [Test, Category("String"), Category("ECS")]
    public void TestGetHashCoded()
    {
        var x = CreateString("test");
        var y = CreateString("test");
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
                    var u = CreateString(str);
                    Assert.AreEqual(str, u.ToManaged());
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => CreateString(str));
                }
            });
    }
}