using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Duplication;
using Xunit;

namespace ReadyM.Api.Generators.Tests;

public sealed class TypeDuplicatorBehaviorTests(ITestOutputHelper output)
{
    private const string AttributeDeclaration = """
using System;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[AttributeUsage(AttributeTargets.Struct)]
internal sealed class DuplicateOfAttribute(Type source) : Attribute
{
    public Type Source { get; } = source;

    public string[]? Exclude { get; set; }
}
""";

    [Fact]
    public void DuplicatedStruct_CopiesMembers_AndHonoursLocalOverridesAndAdditions()
    {
        const string source = """
using System;

namespace ReadyM.Api.Generators.Tests.TestTypes;

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

[DuplicateOf(typeof(Original), Exclude = new[] { "Legacy" })]
public partial struct Duplicate
{
    // An addition that the original does not have.
    public int Tick;

    // A replacement: the copied Describe() is skipped because this one is declared here.
    public string Describe() => "duplicate";
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DuplicateStructTestGenerator>(
            [("Attribute.cs", AttributeDeclaration), ("TestInput.cs", source)],
            output);

        Assert.Empty(result.OutputDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var generated = result.GeneratedSyntaxTrees
            .Single(tree => tree.FilePath.Contains("Duplicate.g.cs"))
            .GetText()
            .ToString();

        // Members are copied with references to the source type remapped onto the duplicate.
        Assert.Contains("public int Health;", generated);
        Assert.Contains("public float Speed;", generated);
        Assert.Contains("public Duplicate(int health, float speed)", generated);
        Assert.Contains("public static Duplicate Default => new Duplicate(100, 1f);", generated);
        Assert.Contains("public static Duplicate operator +(Duplicate left, Duplicate right)", generated);
        Assert.Contains("public Duplicate WithHealth(int health)", generated);
        Assert.Contains("global::System.IEquatable<global::ReadyM.Api.Generators.Tests.TestTypes.Duplicate>", generated);
        Assert.Contains("<summary>Adds two originals together.</summary>", generated);

        // Excluded and locally redeclared members are not copied.
        Assert.DoesNotContain("public int Legacy;", generated);
        Assert.DoesNotContain("\"original\"", generated);

        // No copied member still refers to the source type.
        Assert.DoesNotContain("new Original(", generated);
        Assert.DoesNotContain("Equals(Original ", generated);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var type = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.Duplicate", throwOnError: true)!;

        Assert.NotNull(type.GetField("Health"));
        Assert.NotNull(type.GetField("Tick"));
        Assert.NotNull(type.GetField("Speed"));
        Assert.Null(type.GetField("Legacy"));

        var instance = Activator.CreateInstance(type, 42, 2f)!;
        Assert.Equal(42, type.GetField("Health")!.GetValue(instance));
        Assert.Equal("duplicate", type.GetMethod("Describe")!.Invoke(instance, null));
    }

    [Fact]
    public void NonPartialTarget_IsReportedAsAnIssue()
    {
        const string source = """
namespace ReadyM.Api.Generators.Tests.TestTypes;

public struct Origin
{
    public int Value;
}

public struct SealedCopy
{
}
""";

        var result = Duplicate(source, "Origin", "SealedCopy");

        Assert.Null(result.Source);
        Assert.Equal(TypeDuplicationIssueCode.TargetNotPartial, Assert.Single(result.Issues).Code);
    }

    [Fact]
    public void NonStructSource_IsReportedAsAnIssue()
    {
        const string source = """
namespace ReadyM.Api.Generators.Tests.TestTypes;

public class NotAStruct
{
    public int Value;
}

public partial struct CopyOfClass
{
}
""";

        var result = Duplicate(source, "NotAStruct", "CopyOfClass");

        Assert.Null(result.Source);
        Assert.Equal(TypeDuplicationIssueCode.SourceNotStruct, Assert.Single(result.Issues).Code);
    }

    private TypeDuplicationResult Duplicate(string source, string sourceTypeName, string targetTypeName)
    {
        const string ns = "ReadyM.Api.Generators.Tests.TestTypes.";

        var compilation = SourceGeneratorTestHelper.CreateCompilation(source, output);

        var sourceType = compilation.GetTypeByMetadataName(ns + sourceTypeName)!;
        var targetType = compilation.GetTypeByMetadataName(ns + targetTypeName)!;

        return TypeDuplicator.Duplicate(new TypeDuplicationRequest(compilation, sourceType, targetType));
    }
}
