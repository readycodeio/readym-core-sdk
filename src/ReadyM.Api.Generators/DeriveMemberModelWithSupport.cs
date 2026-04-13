using System;
using ReadyM.Api.Generators.FieldSupport.Cpp;
using ReadyM.Api.Generators.FieldSupport.CSharp;

namespace ReadyM.Api.Generators;

internal sealed class DeriveMemberModelWithSupport(
    DeriveMemberModel model,
    ICSharpFieldTypeSupport? csharpSupport,
    ICppFieldTypeSupport? cppSupport)
{
    public DeriveMemberModel Model { get; } = model ?? throw new ArgumentNullException(nameof(model));
    public ICSharpFieldTypeSupport? CSharpSupport { get; } = csharpSupport;
    public ICppFieldTypeSupport? CppSupport { get; } = cppSupport;

    public bool IsCSharpSupported => CSharpSupport != null;
    public bool IsCppSupported => CppSupport != null;
}