using Xunit;
using Xunit.Abstractions;

namespace ReadyM.Api.Generators.Tests;

public sealed class DeriveCppNativeComponentGeneratorBehaviorTests(ITestOutputHelper output)
{
    [Fact]
public void GeneratedCppFragment_ForRepresentativeNativeComponent_ContainsExpectedAccessorsDirtyMaskAndBackingFields()
{
    const string source = """
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public enum CharacterSex : byte
{
    Unknown = 0,
    Male = 1,
    Female = 2
}

[DeriveINetworkedComponent(emitDirtyMask: false)]
[NativeComponent<AppearanceComponent>]
[StructLayout(LayoutKind.Sequential)]
public partial struct AppearanceComponent : IComponent
{
    private uint _dirtyMask;

    private CharacterSex _sex;
    private int _senescenceLevel;
    private int _customisationEyeMaterialIndex;
    private int _customisationHairIndex;
    private int _customisationEyebrowsIndex;
    private int _customisationMustacheIndex;
    private int _customisationBeardIndex;
}
""";

    var result = SourceGeneratorTestHelper.RunGenerator<DeriveCppNativeComponentGenerator>(source, output);

    var outputErrors = result.OutputDiagnostics
        .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
        .ToArray();

    Assert.Empty(outputErrors);

    var generatedText = string.Join(
        Environment.NewLine,
        result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));

    Assert.Contains("#if GENERATED_CPP_FRAGMENT", generatedText);
    Assert.Contains("RM::Api::Generators::Tests::TestTypes::CharacterSex Sex() const", generatedText);
    Assert.Contains("void SetSex(RM::Api::Generators::Tests::TestTypes::CharacterSex value)", generatedText);
    Assert.Contains("if (_sex != value)", generatedText);
    Assert.Contains("_dirtyMask |= static_cast<uint32_t>(1) << 0;", generatedText);

    Assert.Contains("int32_t SenescenceLevel() const", generatedText);
    Assert.Contains("void SetSenescenceLevel(int32_t value)", generatedText);
    Assert.Contains("_dirtyMask |= static_cast<uint32_t>(1) << 1;", generatedText);

    Assert.Contains("int32_t CustomisationBeardIndex() const", generatedText);
    Assert.Contains("void SetCustomisationBeardIndex(int32_t value)", generatedText);
    Assert.Contains("_dirtyMask |= static_cast<uint32_t>(1) << 6;", generatedText);

    Assert.Contains("private:", generatedText);
    Assert.Contains("uint32_t _dirtyMask = 0; // NOTE: Respecting the user-defined dirty mask size.", generatedText);
    Assert.Contains("CharacterSex _sex = {};", generatedText);
    Assert.Contains("int32_t _senescenceLevel = 0;", generatedText);
    Assert.Contains("int32_t _customisationEyeMaterialIndex = 0;", generatedText);
    Assert.Contains("int32_t _customisationHairIndex = 0;", generatedText);
    Assert.Contains("int32_t _customisationEyebrowsIndex = 0;", generatedText);
    Assert.Contains("int32_t _customisationMustacheIndex = 0;", generatedText);
    Assert.Contains("int32_t _customisationBeardIndex = 0;", generatedText);
    Assert.Contains("#endif", generatedText);
}
}