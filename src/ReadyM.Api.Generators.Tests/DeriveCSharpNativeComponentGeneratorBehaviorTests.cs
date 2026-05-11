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
    
    [Fact]
    public void GeneratedCSharpFragment_Works_ForGenericStructWithoutConstraints()
    {
        const string source = """
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Attributes;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[NativeComponent(bindDelete: true)]
[StructLayout(LayoutKind.Sequential)]
public partial struct AppearanceComponent<T> : IComponent
{
    private uint _dirtyMask;

    private T _value;
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

        Assert.Contains("public partial struct AppearanceComponent<T>", generatedText);
        Assert.Contains(
            "public delegate* unmanaged<void*, Friflo.Engine.ECS.RawEntity, AppearanceComponent<T>*, void> OnEntityDeleteHandler;",
            generatedText);
        Assert.Contains(
            "entity.TryGetComponent<AppearanceComponent<T>>(out var comp)",
            generatedText);
        Assert.Contains(
            "_binding.OnEntityDeleteHandler(_binding.Target, entity.RawEntity, &comp);",
            generatedText);
    }

    [Fact]
    public void GeneratedCSharpFragment_Works_ForGenericStructWithConstraints()
    {
        const string source = """
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Attributes;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public interface IAppearancePayload
{
}

public readonly struct AppearancePayload : IAppearancePayload
{
}

[NativeComponent(bindDelete: true)]
[StructLayout(LayoutKind.Sequential)]
public partial struct AppearanceComponent<TPayload> : IComponent
    where TPayload : unmanaged, IAppearancePayload
{
    private uint _dirtyMask;

    private TPayload _payload;
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

        Assert.Contains("public partial struct AppearanceComponent<TPayload>", generatedText);
        Assert.Contains("where TPayload : unmanaged, IAppearancePayload", generatedText);
        Assert.Contains(
            "public delegate* unmanaged<void*, Friflo.Engine.ECS.RawEntity, AppearanceComponent<TPayload>*, void> OnEntityDeleteHandler;",
            generatedText);
        Assert.Contains(
            "entity.TryGetComponent<AppearanceComponent<TPayload>>(out var comp)",
            generatedText);
        Assert.Contains(
            "_binding.OnEntityDeleteHandler(_binding.Target, entity.RawEntity, &comp);",
            generatedText);
    }
}