using Xunit;

namespace ReadyM.Api.Generators.Tests;

public sealed class DeriveCSharpNativeComponentGeneratorBehaviorTests(ITestOutputHelper output)
{
    [Fact]
    public void GeneratedCSharpFragment_Works()
    {
        const string source = """
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Attributes;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[NativeComponent(bindDelete: true)]
[StructLayout(LayoutKind.Sequential)]
public partial struct AppearanceComponent : IComponent
{
    private uint _dirtyMask;

    private int _a;
    private byte _b;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveCSharpNativeComponentGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var generatedText = string.Join(
            Environment.NewLine,
            result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));

    }
}