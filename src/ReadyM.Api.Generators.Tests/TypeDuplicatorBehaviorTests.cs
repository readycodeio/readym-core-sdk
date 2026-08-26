using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Duplication;
using Xunit;

namespace ReadyM.Api.Generators.Tests;

public sealed class TypeDuplicatorBehaviorTests(ITestOutputHelper output)
{
    private const string Namespace = "ReadyM.Api.Generators.Tests.TestTypes";

    private const string AttributeDeclaration = """
using System;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[AttributeUsage(AttributeTargets.Struct)]
internal sealed class DuplicateAsAttribute(string targetName) : Attribute
{
    public string TargetName { get; } = targetName;

    public string[]? Exclude { get; set; }
}
""";

    private const string Original = """
using System;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DuplicateAs("Duplicate", Exclude = new[] { "Legacy" })]
public partial struct Original : IEquatable<Original>
{
    public int Health;
    public float Speed;

    // Nothing else references this, so it can be excluded cleanly.
    public int Legacy;

    public Original(int health, float speed)
    {
        Health = health;
        Speed = speed;
    }

    public static Original Default => new Original(100, 1f);

    /// <summary>Adds two originals together.</summary>
    public static Original operator +(Original left, Original right)
        => new Original(left.Health + right.Health, left.Speed + right.Speed);

    public Original WithHealth(int health) => new Original(health, Speed);

    public bool Equals(Original other) => Health == other.Health && Speed.Equals(other.Speed);

    public string Describe() => "original";
}
""";

    [Fact]
    public void TargetThatDoesNotExist_IsBroughtIntoExistence()
    {
        // Nothing anywhere declares Duplicate. The whole type comes from this call.
        var result = Run([("Attribute.cs", AttributeDeclaration), ("Original.cs", Original)]);

        var generated = Generated(result, "Duplicate");

        Assert.Contains("public partial struct Duplicate", generated);
        Assert.Contains("namespace ReadyM.Api.Generators.Tests.TestTypes;", generated);

        // Members are copied with references to the source type remapped onto the duplicate.
        Assert.Contains("public int Health;", generated);
        Assert.Contains("public float Speed;", generated);
        Assert.Contains("public Duplicate(int health, float speed)", generated);
        Assert.Contains("public static Duplicate Default => new Duplicate(100, 1f);", generated);
        Assert.Contains("public static Duplicate operator +(Duplicate left, Duplicate right)", generated);
        Assert.Contains("public Duplicate WithHealth(int health)", generated);
        Assert.Contains($"global::System.IEquatable<global::{Namespace}.Duplicate>", generated);
        Assert.Contains("<summary>Adds two originals together.</summary>", generated);

        Assert.DoesNotContain("public int Legacy;", generated);
        Assert.DoesNotContain("new Original(", generated);

        var type = Load(result, "Duplicate");

        Assert.NotNull(type.GetField("Health"));
        Assert.NotNull(type.GetField("Speed"));
        Assert.Null(type.GetField("Legacy"));

        var instance = Activator.CreateInstance(type, 42, 2f)!;
        Assert.Equal(42, type.GetField("Health")!.GetValue(instance));
        Assert.Equal("original", type.GetMethod("Describe")!.Invoke(instance, null));
    }

    [Fact]
    public void GenericSource_CarriesTypeParametersAndConstraintsAcross()
    {
        const string source = """
using System;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DuplicateAs("Crate")]
public partial struct Box<T> : IEquatable<Box<T>>
    where T : struct, IComparable<T>
{
    public T Value;

    public Box(T value) => Value = value;

    public Box<T> Replace(T value) => new Box<T>(value);

    public bool Equals(Box<T> other) => Value.Equals(other.Value);
}
""";

        var result = Run([("Attribute.cs", AttributeDeclaration), ("Box.cs", source)]);

        var generated = Generated(result, "Crate");

        Assert.Contains("public partial struct Crate<T>", generated);
        Assert.Contains("where T : struct, IComparable<T>", generated);
        Assert.Contains("public Crate<T> Replace(T value) => new Crate<T>(value);", generated);
        Assert.Contains($"global::System.IEquatable<global::{Namespace}.Crate<T>>", generated);

        var type = Load(result, "Crate`1").MakeGenericType(typeof(int));
        var instance = Activator.CreateInstance(type, 7)!;

        Assert.Equal(7, type.GetField("Value")!.GetValue(instance));
    }

    [Fact]
    public void ExistingPartialHalf_ReplacesCopiedMembersAndAddsItsOwn()
    {
        const string half = """
namespace ReadyM.Api.Generators.Tests.TestTypes;

public partial struct Duplicate
{
    // An addition the original does not have.
    public int Tick;

    // A replacement: the copied Describe() is skipped because this one is declared here.
    public string Describe() => "duplicate";
}
""";

        var result = Run([("Attribute.cs", AttributeDeclaration), ("Original.cs", Original), ("Half.cs", half)]);

        var generated = Generated(result, "Duplicate");

        Assert.Contains("public int Health;", generated);
        Assert.DoesNotContain("\"original\"", generated);
        Assert.DoesNotContain("public int Tick;", generated);

        var type = Load(result, "Duplicate");

        Assert.NotNull(type.GetField("Tick"));

        var instance = Activator.CreateInstance(type, 42, 2f)!;
        Assert.Equal("duplicate", type.GetMethod("Describe")!.Invoke(instance, null));
    }

    [Fact]
    public void ExistingNonPartialTarget_IsGeneratedAnywayAndLeftToTheCompiler()
    {
        const string source = """
namespace ReadyM.Api.Generators.Tests.TestTypes;

public struct Origin
{
    public int Value;
}

public struct Copy
{
}
""";

        var generated = Duplicate(source, "Origin", "Copy");

        // A clash with a hand-written declaration is the programmer's to resolve, so nothing is said about it.
        Assert.DoesNotContain("#error", generated);
        Assert.Contains("public partial struct Copy", generated);
        Assert.Contains("public int Value;", generated);
    }

    [Fact]
    public void NonStructSource_StillGeneratesAndSaysWhyInline()
    {
        const string source = """
namespace ReadyM.Api.Generators.Tests.TestTypes;

public class NotAStruct
{
    public int Value;
}
""";

        var generated = Duplicate(source, "NotAStruct", "CopyOfClass");

        Assert.Contains("#error", generated);
        Assert.Contains("is not a struct", generated);

        // The copy is still emitted: the #error names the problem, it does not withhold the work.
        Assert.Contains("public partial struct CopyOfClass", generated);
        Assert.Contains("public int Value;", generated);
    }

    [Fact]
    public void TargetNameMatchingTheSource_SaysSoInline()
    {
        const string source = """
namespace ReadyM.Api.Generators.Tests.TestTypes;

public partial struct Origin
{
    public int Value;
}
""";

        var generated = Duplicate(source, "Origin", "Origin");

        Assert.Contains("#error", generated);
        Assert.Contains("cannot be a duplicate of itself", generated);
    }

    [Fact]
    public void UnusableTargetName_SaysSoInlineAndEmitsNoType()
    {
        const string source = """
namespace ReadyM.Api.Generators.Tests.TestTypes;

public partial struct Origin
{
    public int Value;
}
""";

        var generated = Duplicate(source, "Origin", "not a name");

        Assert.Contains("#error", generated);
        Assert.Contains("is not a usable type name", generated);
        Assert.DoesNotContain("struct", generated);
    }

    [Fact]
    public void GeneratedErrors_AreReportedByTheCompiler()
    {
        const string source = """
using System;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DuplicateAs("Origin")]
public partial struct Origin
{
    public int Value;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DuplicateStructTestGenerator>(
            [("Attribute.cs", AttributeDeclaration), ("Origin.cs", source)],
            output);

        // CS1029 is the compiler acting on our #error, which is the whole point of putting it in the file.
        Assert.Contains(result.OutputDiagnostics, d => d.Id == "CS1029");
    }

    [Fact]
    public void PrimaryConstructor_IsCarriedOntoTheTarget()
    {
        const string source = """
using System;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DuplicateAs("Spot")]
public partial struct Point(int x, int y)
{
    // A primary constructor parameter is in scope here, so the parameter list has to come across with it.
    public int X = x;

    public int Sum => x + y;

    public Point Shifted() => new Point(x + 1, y + 1);

    public Point(int both) : this(both, both)
    {
    }
}
""";

        var result = Run([("Attribute.cs", AttributeDeclaration), ("Point.cs", source)]);

        var generated = Generated(result, "Spot");

        Assert.Contains("public partial struct Spot(int x, int y)", generated);
        Assert.Contains("public int X = x;", generated);
        Assert.Contains("public int Sum => x + y;", generated);
        Assert.Contains("public Spot Shifted() => new Spot(x + 1, y + 1);", generated);
        Assert.Contains("public Spot(int both) : this(both, both)", generated);

        var type = Load(result, "Spot");
        var instance = Activator.CreateInstance(type, 3, 4)!;

        Assert.Equal(3, type.GetField("X")!.GetValue(instance));
        Assert.Equal(7, type.GetProperty("Sum")!.GetValue(instance));

        var chained = Activator.CreateInstance(type, 5)!;
        Assert.Equal(5, type.GetField("X")!.GetValue(chained));
    }

    [Fact]
    public void PositionalRecordStruct_KeepsItsParametersAndKind()
    {
        const string source = """
namespace ReadyM.Api.Generators.Tests.TestTypes;

[DuplicateAs("Coord")]
public partial record struct Pair(int A, int B);
""";

        var result = Run([("Attribute.cs", AttributeDeclaration), ("Pair.cs", source)]);

        var generated = Generated(result, "Coord");

        Assert.Contains("public partial record struct Coord(int A, int B)", generated);

        var type = Load(result, "Coord");
        var instance = Activator.CreateInstance(type, 1, 2)!;

        Assert.Equal(1, type.GetProperty("A")!.GetValue(instance));
        Assert.Equal(2, type.GetProperty("B")!.GetValue(instance));
    }

    [Fact]
    public void ExistingHalfWithItsOwnPrimaryConstructor_KeepsIt()
    {
        const string source = """
namespace ReadyM.Api.Generators.Tests.TestTypes;

[DuplicateAs("Slot")]
public partial struct Cell(int index)
{
    public int Doubled() => index * 2;
}
""";

        const string half = """
namespace ReadyM.Api.Generators.Tests.TestTypes;

public partial struct Slot(int index)
{
    public int Index => index;
}
""";

        var result = Run([("Attribute.cs", AttributeDeclaration), ("Cell.cs", source), ("Half.cs", half)]);

        var generated = Generated(result, "Slot");

        // Only one part may declare the parameter list, so the hand-written half keeps it.
        Assert.Contains("public partial struct Slot", generated);
        Assert.DoesNotContain("Slot(int index)", generated);
        Assert.Contains("public int Doubled() => index * 2;", generated);

        var type = Load(result, "Slot");
        var instance = Activator.CreateInstance(type, 6)!;

        Assert.Equal(12, type.GetMethod("Doubled")!.Invoke(instance, null));
        Assert.Equal(6, type.GetProperty("Index")!.GetValue(instance));
    }

    private SourceGeneratorTestHelper.GeneratorRunResult Run(IEnumerable<(string Path, string Source)> sources)
    {
        var result = SourceGeneratorTestHelper.RunGenerator<DuplicateStructTestGenerator>(sources, output);

        Assert.Empty(result.OutputDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        return result;
    }

    private static string Generated(SourceGeneratorTestHelper.GeneratorRunResult result, string targetName)
        => result.GeneratedSyntaxTrees
            .Single(tree => tree.FilePath.EndsWith($"{Namespace}.{targetName}.g.cs", StringComparison.Ordinal))
            .GetText()
            .ToString();

    private Type Load(SourceGeneratorTestHelper.GeneratorRunResult result, string targetName)
        => SourceGeneratorTestHelper
            .EmitToAssembly(result.OutputCompilation, output)
            .GetType($"{Namespace}.{targetName}", throwOnError: true)!;

    private string Duplicate(string source, string sourceTypeName, string targetName)
    {
        var compilation = SourceGeneratorTestHelper.CreateCompilation(source, output);
        var sourceType = compilation.GetTypeByMetadataName($"{Namespace}.{sourceTypeName}")!;

        var generated = TypeDuplicator.Duplicate(new TypeDuplicationRequest(compilation, sourceType, targetName));

        output.WriteLine(generated);

        return generated;
    }
}
