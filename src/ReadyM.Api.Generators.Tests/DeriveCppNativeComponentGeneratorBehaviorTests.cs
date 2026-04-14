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

[DeriveINetworkedComponent(emitDirtyMask: false), NativeComponent]
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
        Assert.Contains("RM::Generators::Tests::TestTypes::CharacterSex Sex() const", generatedText);
        Assert.Contains("void SetSex(RM::Generators::Tests::TestTypes::CharacterSex value)", generatedText);
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

    [Fact]
    public void GeneratedCppFragment_ForNativeComponentWithListFields_ContainsExpectedListAccessorsSettersDirtyMaskAndBackingFields()
    {
        const string source = """
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.Generators;
using Yooni.Native.Container;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public enum CharacterSex : byte
{
    Unknown = 0,
    Male = 1,
    Female = 2
}

public struct Pair
{
    public int X;
    public int Y;
}

[DeriveINetworkedComponent(emitDirtyMask: false), NativeComponent]
[StructLayout(LayoutKind.Sequential)]
public partial struct AppearanceComponent : IComponent
{
    private uint _dirtyMask;

    private NativeList<int> _intList;
    private NativeList<CharacterSex> _sexList;
    private NativeList<Pair> _pairList;
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

        AssertContainerMember(
            generatedText,
            getterSignature: "Yooni::Native::Container::NativeList<int32_t> IntList() const",
            setterSignature: "void SetIntList(Yooni::Native::Container::NativeList<int32_t> value)",
            inequalityGuard: "if (_intList != value)",
            assignment: "_intList = value;",
            dirtyMaskBit: 0,
            backingField: "Yooni::Native::Container::NativeList<int32_t> _intList = {};");

        AssertContainerMember(
            generatedText,
            getterSignature: "Yooni::Native::Container::NativeList<RM::Generators::Tests::TestTypes::CharacterSex> SexList() const",
            setterSignature: "void SetSexList(Yooni::Native::Container::NativeList<RM::Generators::Tests::TestTypes::CharacterSex> value)",
            inequalityGuard: "if (_sexList != value)",
            assignment: "_sexList = value;",
            dirtyMaskBit: 1,
            backingField: "Yooni::Native::Container::NativeList<RM::Generators::Tests::TestTypes::CharacterSex> _sexList = {};");

        AssertContainerMember(
            generatedText,
            getterSignature: "Yooni::Native::Container::NativeList<RM::Generators::Tests::TestTypes::Pair> PairList() const",
            setterSignature: "void SetPairList(Yooni::Native::Container::NativeList<RM::Generators::Tests::TestTypes::Pair> value)",
            inequalityGuard: "if (_pairList != value)",
            assignment: "_pairList = value;",
            dirtyMaskBit: 2,
            backingField: "Yooni::Native::Container::NativeList<RM::Generators::Tests::TestTypes::Pair> _pairList = {};");

        Assert.Contains("private:", generatedText);
        Assert.Contains("uint32_t _dirtyMask = 0; // NOTE: Respecting the user-defined dirty mask size.", generatedText);
        Assert.Contains("#endif", generatedText);
    }

    [Fact]
    public void GeneratedCppFragment_ForNativeComponentWithFixedFields_ContainsExpectedFixedAccessorsSettersDirtyMaskAndBackingFields()
    {
        const string source = """
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.Generators;
using Yooni.Native.Container;
using Yooni.Native.LowLevel;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public enum CharacterSex : byte
{
    Unknown = 0,
    Male = 1,
    Female = 2
}

public struct Pair
{
    public int X;
    public int Y;
}

[DeriveINetworkedComponent(emitDirtyMask: false), NativeComponent]
[StructLayout(LayoutKind.Sequential)]
public partial struct FixedComponent : IComponent
{
    private uint _dirtyMask;

    private NativeFixed<int, Storage8<int>> _intFixed;
    private NativeFixed<CharacterSex, Storage16<CharacterSex>> _sexFixed;
    private NativeFixed<Pair, Storage32<Pair>> _pairFixed;
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

        AssertContainerMember(
            generatedText,
            getterSignature: "Yooni::Native::Container::NativeFixed<int32_t, 8> IntFixed() const",
            setterSignature: "void SetIntFixed(Yooni::Native::Container::NativeFixed<int32_t, 8> value)",
            inequalityGuard: "if (_intFixed != value)",
            assignment: "_intFixed = value;",
            dirtyMaskBit: 0,
            backingField: "Yooni::Native::Container::NativeFixed<int32_t, 8> _intFixed = {};");

        AssertContainerMember(
            generatedText,
            getterSignature: "Yooni::Native::Container::NativeFixed<RM::Generators::Tests::TestTypes::CharacterSex, 16> SexFixed() const",
            setterSignature: "void SetSexFixed(Yooni::Native::Container::NativeFixed<RM::Generators::Tests::TestTypes::CharacterSex, 16> value)",
            inequalityGuard: "if (_sexFixed != value)",
            assignment: "_sexFixed = value;",
            dirtyMaskBit: 1,
            backingField: "Yooni::Native::Container::NativeFixed<RM::Generators::Tests::TestTypes::CharacterSex, 16> _sexFixed = {};");

        AssertContainerMember(
            generatedText,
            getterSignature: "Yooni::Native::Container::NativeFixed<RM::Generators::Tests::TestTypes::Pair, 32> PairFixed() const",
            setterSignature: "void SetPairFixed(Yooni::Native::Container::NativeFixed<RM::Generators::Tests::TestTypes::Pair, 32> value)",
            inequalityGuard: "if (_pairFixed != value)",
            assignment: "_pairFixed = value;",
            dirtyMaskBit: 2,
            backingField: "Yooni::Native::Container::NativeFixed<RM::Generators::Tests::TestTypes::Pair, 32> _pairFixed = {};");
    }

    [Fact]
    public void GeneratedCppFragment_ForNativeComponentWithDictionaryFields_ContainsExpectedDictionaryAccessorsSettersDirtyMaskAndBackingFields()
    {
        const string source = """
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.Generators;
using Yooni.Native.Container;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public enum CharacterSex : byte
{
    Unknown = 0,
    Male = 1,
    Female = 2
}

public struct Pair
{
    public int X;
    public int Y;
}

public struct CharacterSexHash : IHashFunction<CharacterSex>
{
    public uint ComputeHash(CharacterSex value)
    {
        return (uint)value;
    }
}

public struct PairHash : IHashFunction<Pair>
{
    public uint ComputeHash(Pair value)
    {
        return (uint)(value.X * 397 ^ value.Y);
    }
}

[DeriveINetworkedComponent(emitDirtyMask: false), NativeComponent]
[StructLayout(LayoutKind.Sequential)]
public partial struct DictionaryComponent : IComponent
{
    private uint _dirtyMask;

    private NativeDictionary<int, CharacterSex, MemoryHash<int>> _intToSex;
    private NativeDictionary<CharacterSex, Pair, CharacterSexHash> _sexToPair;
    private NativeDictionary<Pair, int, PairHash> _pairToInt;
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

        AssertContainerMember(
            generatedText,
            getterSignature: "Yooni::Native::Container::NativeDictionary<int32_t, RM::Generators::Tests::TestTypes::CharacterSex, Yooni::Native::Container::MemoryHash<int32_t>> IntToSex() const",
            setterSignature: "void SetIntToSex(Yooni::Native::Container::NativeDictionary<int32_t, RM::Generators::Tests::TestTypes::CharacterSex, Yooni::Native::Container::MemoryHash<int32_t>> value)",
            inequalityGuard: "if (_intToSex != value)",
            assignment: "_intToSex = value;",
            dirtyMaskBit: 0,
            backingField: "Yooni::Native::Container::NativeDictionary<int32_t, RM::Generators::Tests::TestTypes::CharacterSex, Yooni::Native::Container::MemoryHash<int32_t>> _intToSex = {};");

        AssertContainerMember(
            generatedText,
            getterSignature: "Yooni::Native::Container::NativeDictionary<RM::Generators::Tests::TestTypes::CharacterSex, RM::Generators::Tests::TestTypes::Pair, RM::Generators::Tests::TestTypes::CharacterSexHash> SexToPair() const",
            setterSignature: "void SetSexToPair(Yooni::Native::Container::NativeDictionary<RM::Generators::Tests::TestTypes::CharacterSex, RM::Generators::Tests::TestTypes::Pair, RM::Generators::Tests::TestTypes::CharacterSexHash> value)",
            inequalityGuard: "if (_sexToPair != value)",
            assignment: "_sexToPair = value;",
            dirtyMaskBit: 1,
            backingField: "Yooni::Native::Container::NativeDictionary<RM::Generators::Tests::TestTypes::CharacterSex, RM::Generators::Tests::TestTypes::Pair, RM::Generators::Tests::TestTypes::CharacterSexHash> _sexToPair = {};");

        AssertContainerMember(
            generatedText,
            getterSignature: "Yooni::Native::Container::NativeDictionary<RM::Generators::Tests::TestTypes::Pair, int32_t, RM::Generators::Tests::TestTypes::PairHash> PairToInt() const",
            setterSignature: "void SetPairToInt(Yooni::Native::Container::NativeDictionary<RM::Generators::Tests::TestTypes::Pair, int32_t, RM::Generators::Tests::TestTypes::PairHash> value)",
            inequalityGuard: "if (_pairToInt != value)",
            assignment: "_pairToInt = value;",
            dirtyMaskBit: 2,
            backingField: "Yooni::Native::Container::NativeDictionary<RM::Generators::Tests::TestTypes::Pair, int32_t, RM::Generators::Tests::TestTypes::PairHash> _pairToInt = {};");
    }

    [Fact]
    public void GeneratedCppFragment_ForNativeComponentWithRingBufferFields_ContainsExpectedRingBufferAccessorsSettersDirtyMaskAndBackingFields()
    {
        const string source = """
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.Generators;
using Yooni.Native.Container;
using Yooni.Native.LowLevel;

namespace ReadyM.Api.Generators.Tests.TestTypes;

public enum CharacterSex : byte
{
    Unknown = 0,
    Male = 1,
    Female = 2
}

public struct Pair
{
    public int X;
    public int Y;
}

[DeriveINetworkedComponent(emitDirtyMask: false), NativeComponent]
[StructLayout(LayoutKind.Sequential)]
public partial struct RingBufferComponent : IComponent
{
    private uint _dirtyMask;

    private NativeRingBuffer<int, Storage8<int>> _intHistory;
    private NativeRingBuffer<CharacterSex, Storage16<CharacterSex>> _sexHistory;
    private NativeRingBuffer<Pair, Storage32<Pair>> _pairHistory;
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

        AssertContainerMember(
            generatedText,
            getterSignature: "Yooni::Native::Container::NativeRingBuffer<int32_t, 8> IntHistory() const",
            setterSignature: "void SetIntHistory(Yooni::Native::Container::NativeRingBuffer<int32_t, 8> value)",
            inequalityGuard: "if (_intHistory != value)",
            assignment: "_intHistory = value;",
            dirtyMaskBit: 0,
            backingField: "Yooni::Native::Container::NativeRingBuffer<int32_t, 8> _intHistory = {};");

        AssertContainerMember(
            generatedText,
            getterSignature: "Yooni::Native::Container::NativeRingBuffer<RM::Generators::Tests::TestTypes::CharacterSex, 16> SexHistory() const",
            setterSignature: "void SetSexHistory(Yooni::Native::Container::NativeRingBuffer<RM::Generators::Tests::TestTypes::CharacterSex, 16> value)",
            inequalityGuard: "if (_sexHistory != value)",
            assignment: "_sexHistory = value;",
            dirtyMaskBit: 1,
            backingField: "Yooni::Native::Container::NativeRingBuffer<RM::Generators::Tests::TestTypes::CharacterSex, 16> _sexHistory = {};");

        AssertContainerMember(
            generatedText,
            getterSignature: "Yooni::Native::Container::NativeRingBuffer<RM::Generators::Tests::TestTypes::Pair, 32> PairHistory() const",
            setterSignature: "void SetPairHistory(Yooni::Native::Container::NativeRingBuffer<RM::Generators::Tests::TestTypes::Pair, 32> value)",
            inequalityGuard: "if (_pairHistory != value)",
            assignment: "_pairHistory = value;",
            dirtyMaskBit: 2,
            backingField: "Yooni::Native::Container::NativeRingBuffer<RM::Generators::Tests::TestTypes::Pair, 32> _pairHistory = {};");
    }

    [Fact]
    public void GeneratedCppFragment_ForNativeComponentWithNativeStringFields_ContainsExpectedStringAccessorsSettersDirtyMaskAndBackingFields()
    {
        const string source = """
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.Generators;
using Yooni.Native.Container;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent(emitDirtyMask: false), NativeComponent]
[StructLayout(LayoutKind.Sequential)]
public partial struct StringComponent : IComponent
{
    private uint _dirtyMask;

    private NativeString64 _displayName;
    private NativeString64 _title;
    private NativeString256 _biography;
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

        AssertContainerMember(
            generatedText,
            getterSignature: "Yooni::Native::Container::NativeString64 DisplayName() const",
            setterSignature: "void SetDisplayName(Yooni::Native::Container::NativeString64 value)",
            inequalityGuard: "if (_displayName != value)",
            assignment: "_displayName = value;",
            dirtyMaskBit: 0,
            backingField: "Yooni::Native::Container::NativeString64 _displayName = {};");

        AssertContainerMember(
            generatedText,
            getterSignature: "Yooni::Native::Container::NativeString64 Title() const",
            setterSignature: "void SetTitle(Yooni::Native::Container::NativeString64 value)",
            inequalityGuard: "if (_title != value)",
            assignment: "_title = value;",
            dirtyMaskBit: 1,
            backingField: "Yooni::Native::Container::NativeString64 _title = {};");

        AssertContainerMember(
            generatedText,
            getterSignature: "Yooni::Native::Container::NativeString256 Biography() const",
            setterSignature: "void SetBiography(Yooni::Native::Container::NativeString256 value)",
            inequalityGuard: "if (_biography != value)",
            assignment: "_biography = value;",
            dirtyMaskBit: 2,
            backingField: "Yooni::Native::Container::NativeString256 _biography = {};");
    }

    private static void AssertContainerMember(
        string generatedText,
        string getterSignature,
        string setterSignature,
        string inequalityGuard,
        string assignment,
        int dirtyMaskBit,
        string backingField)
    {
        Assert.Contains(getterSignature, generatedText);
        Assert.Contains(setterSignature, generatedText);
        Assert.Contains(inequalityGuard, generatedText);
        Assert.Contains(assignment, generatedText);
        Assert.Contains($"_dirtyMask |= static_cast<uint32_t>(1) << {dirtyMaskBit};", generatedText);
        Assert.Contains(backingField, generatedText);
    }
}