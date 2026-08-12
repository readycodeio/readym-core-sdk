using Xunit;

namespace ReadyM.Api.Generators.Tests;

public sealed class DeriveNativeBindingGeneratorBehaviorTests(ITestOutputHelper output)
{
    [Fact]
    public void GeneratedNativeBindingValidation_Works()
    {
        const string source = """
using System;
using ReadyM.Api.Attributes;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveNativeBinding]
public unsafe partial struct NativeBinding
{
    private int* _items;
    private delegate* unmanaged<void> _tick;
    private IntPtr _target;
    private UIntPtr _cookie;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveNativeBindingGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var generatedText = string.Join(
            Environment.NewLine,
            result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));

        Assert.Contains("public unsafe partial struct NativeBinding", generatedText);
        Assert.Contains("public bool IsValid", generatedText);
        Assert.Contains("_items != null", generatedText);
        Assert.Contains("_tick != null", generatedText);
        Assert.Contains("_target != System.IntPtr.Zero", generatedText);
        Assert.Contains("_cookie != System.UIntPtr.Zero", generatedText);
        Assert.Contains("public void EnsureValid()", generatedText);
        Assert.Contains("EnsureValid(nameof(NativeBinding));", generatedText);
        Assert.Contains("if (_items == null)", generatedText);
        Assert.Contains("if (_tick == null)", generatedText);
        Assert.Contains("if (_target == System.IntPtr.Zero)", generatedText);
        Assert.Contains("if (_cookie == System.UIntPtr.Zero)", generatedText);
    }

    [Fact]
    public void GeneratedNativeBindingValidation_Works_ForNestedStructFieldWithoutAttribute()
    {
        const string source = """
using ReadyM.Api.Attributes;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public partial struct ChildBinding
{
    public bool IsValid => true;

    public void EnsureValid(string path)
    {
    }
}

[DeriveNativeBinding]
public partial struct ParentBinding
{
    private ChildBinding _child;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveNativeBindingGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var generatedText = string.Join(
            Environment.NewLine,
            result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));

        Assert.Contains("public unsafe partial struct ParentBinding", generatedText);
        Assert.Contains("_child.IsValid", generatedText);
        Assert.Contains("_child.EnsureValid($\"{path}.{nameof(_child)}\");", generatedText);
    }

    [Fact]
    public void GeneratedNativeBindingValidation_Works_ForNestedTypes()
    {
        const string source = """
using System;
using ReadyM.Api.Attributes;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public partial class NativeBindingHost
{
    public partial class NativeBindingContainer
    {
        [DeriveNativeBinding]
        public partial struct NestedBinding
        {
            private IntPtr _target;
        }
    }
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveNativeBindingGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var generatedText = string.Join(
            Environment.NewLine,
            result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));

        Assert.Contains("public partial class NativeBindingHost", generatedText);
        Assert.Contains("public partial class NativeBindingContainer", generatedText);
        Assert.Contains("public unsafe partial struct NestedBinding", generatedText);
        Assert.Contains("_target != System.IntPtr.Zero", generatedText);
        Assert.Contains("if (_target == System.IntPtr.Zero)", generatedText);
    }

    [Fact]
    public void GeneratedNativeBindingValidation_Works_ForGenericStruct()
    {
        const string source = """
using ReadyM.Api.Attributes;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public interface INativeBindingPayload
{
    bool IsValid { get; }
    void EnsureValid(string path);
}

public partial struct NativeBindingPayload : INativeBindingPayload
{
    public bool IsValid => true;

    public void EnsureValid(string path)
    {
    }
}

[DeriveNativeBinding]
public partial struct NativeBinding<TPayload>
    where TPayload : struct, INativeBindingPayload
{
    private TPayload _payload;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveNativeBindingGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(outputErrors);

        var generatedText = string.Join(
            Environment.NewLine,
            result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));

        Assert.Contains("public unsafe partial struct NativeBinding<TPayload>", generatedText);
        Assert.Contains("_payload.IsValid", generatedText);
        Assert.Contains("_payload.EnsureValid($\"{path}.{nameof(_payload)}\");", generatedText);
    }

    [Fact]
    public void GeneratedNativeBindingValidation_ReportsDiagnostic_ForUnsupportedField()
    {
        const string source = """
using ReadyM.Api.Attributes;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveNativeBinding]
public partial struct InvalidNativeBinding
{
    private string _name;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveNativeBindingGenerator>(source, output);

        var outputErrors = result.OutputDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.NotEmpty(outputErrors);
    }
}