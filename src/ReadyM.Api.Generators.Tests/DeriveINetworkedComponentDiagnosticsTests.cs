using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace ReadyM.Api.Generators.Tests;

public sealed class DeriveINetworkedComponentDiagnosticsTests(ITestOutputHelper output)
{
    [Fact]
    public void UnsupportedFieldTypeEmitsCompilationError()
    {
        const string source = """
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public struct UnsupportedType
{
  public int Value;
}

[DeriveINetworkedComponent]
public partial struct UnsupportedComponent
{
  private UnsupportedType _value;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        var errors = result.OutputDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.NotEmpty(errors);

        Assert.Contains(
            errors,
            diagnostic =>
                diagnostic.ToString().Contains("#error"));
    }
}