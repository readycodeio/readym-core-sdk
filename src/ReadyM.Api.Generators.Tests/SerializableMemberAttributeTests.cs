using Xunit;

namespace ReadyM.Api.Generators.Tests;

public sealed class SerializableMemberAttributeTests(ITestOutputHelper output)
{
    private const string Source = """
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetSerializable]
public partial struct Picked
{
    public int Kept;
    [ExcludeSerializable] public int Skipped;
    [IncludeSerializable] internal int Forced;
}
""";

    [Fact]
    public void ExcludeSerializable_LeavesThePublicFieldOut()
    {
        var generated = Generate();
        Assert.Contains("Kept", generated);
        Assert.DoesNotContain("Skipped", generated);
    }

    [Fact]
    public void IncludeSerializable_MapsTheInternalField()
    {
        Assert.Contains("Forced", Generate());
    }

    private string Generate()
    {
        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetSerializableGenerator>(Source, output);
        Assert.Empty(result.OutputDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        return string.Join("\n", result.DriverRunResult.Results.SelectMany(r => r.GeneratedSources).Select(s => s.SourceText.ToString()));
    }
}
