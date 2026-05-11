using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReadyM.Api.Generators.Derive.Cpp;
using ReadyM.Api.Generators.Derive.Cpp.FieldSupport;
using static ReadyM.Api.Generators.DeriveCppUtils;

namespace ReadyM.Api.Generators;

[Generator]
public sealed class DeriveCppNativeComponentGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var outputPathProvider =
            context.AnalyzerConfigOptionsProvider
                .Select((options, _) =>
                {
                    var genCppPath = options.GlobalOptions.TryGetValue(
                        "build_property.GeneratedCppPath",
                        out var path)
                        ? path
                        : null;
                    var genCppEnabled = options.GlobalOptions.TryGetValue(
                        "build_property.GeneratedCppEnabled",
                        out var enabled) && enabled == "true";
                    (string? GenCppPath, bool GenCppEnabled) opts = (genCppPath, genCppEnabled);
                    return opts;
                });
        var codeProvider = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform);
        var combined = codeProvider.Combine(outputPathProvider);
        
        context.RegisterSourceOutput(
            combined,
            (spc, result) =>
            {
                // NOTE: For debugging purposes
                var source = result.Left.Code;
                // Replace /* with (* and */ with *)
                source = System.Text.RegularExpressions.Regex.Replace(source, @"/\*", "(*");
                source = System.Text.RegularExpressions.Regex.Replace(source, @"\*/", "*)");
                
                spc.AddSource(result.Left.Name + ".g.h", $"/*\n{source}\n*/");

                if (!result.Right.GenCppEnabled)
                    return;
                
                if (result.Right.GenCppPath == null)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "CPP002",
                            title: "Cpp generator output path",
                            messageFormat: "Cpp generator output path is empty",
                            category: "Generator",
                            defaultSeverity: DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        Location.None));

                    return;
                }
                
                var cppGenPath = Path.GetFullPath(result.Right.GenCppPath);
                var fileName = result.Left.Name + ".g.h";
                var fullFileName = Path.Combine(cppGenPath, fileName);
                
                spc.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "CPP001",
                        title: "Cpp generator output",
                        messageFormat: "Generated file = '{0}'",
                        category: "Generator",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    Location.None,
                    fullFileName));
                
                Directory.CreateDirectory(cppGenPath);
                using var file = File.CreateText(fullFileName);
                file.Write(result.Left.Code);
            });
    }

    private bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return false;

        var structDecl = syntaxNode as StructDeclarationSyntax;
        if (structDecl == null || structDecl.AttributeLists.Count == 0)
            return false;

        var attributes = structDecl.AttributeLists.SelectMany(static x => x.Attributes).ToList();
        return attributes.Any(x => x.Name is IdentifierNameSyntax { Identifier.Text: "NativeComponent" or "NativeComponentAttribute" });
    }

    private (string Name, string Code) Transform(GeneratorSyntaxContext context, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return (string.Empty, string.Empty);

        var symbol = DeriveUtils.GetAttributedSymbol(context, ct);
        var targetModel = DeriveComponentUtils.GetTargetModel(symbol, context);
        var code = GenerateCppFragment(targetModel);
        var genName = DeriveUtils.GetGeneratedFileName(symbol) + "GeneratedBase";

        return (genName, code);
    }

    private string GenerateCppFragment(DeriveTargetModel model)
    {
        var info = model.Source;
        var members = model.Members;
        var maskInfo = model.MaskInfo;

        if (maskInfo != null)
        {
            if (maskInfo.Bits < members.Count)
            {
                if (model.Source.EmitDirtyMask)
                {
                    maskInfo.AddError(
                        $"Too many synced fields in `{model.Source.Name}` " +
                        $"to fit in a dirty mask. Maximum supported is {maskInfo.Bits.ToString(CultureInfo.InvariantCulture)}" +
                        $", but {members.Count.ToString(CultureInfo.InvariantCulture)} were found.");
                }
                else
                {
                    maskInfo.AddError(
                        $"Too many synced members in `{model.Source.Name}` " +
                        $"to fit in the user-supplied _dirtyMask. Maximum supported is {maskInfo.Bits.ToString(CultureInfo.InvariantCulture)}" +
                        $", but {members.Count.ToString(CultureInfo.InvariantCulture)} were found.");
                }
            }
        }
        
        foreach (var member in members)
        {
            if (!HasGetEmitFieldSupportImpl(member, false))
            {
                member.Source.AddError($"Unsupported type '{member.Source.Type.ToDisplayString()}' for networked member '{member.Source.Name}'.");
            }
        }
        
        var sb = new StringBuilder();
        var moduleState = new CppModuleState();

        AddDefaultIncludes(moduleState, model);
        
        var ns = CppTypeNamespace(model.Source.Symbol);

        if (!string.IsNullOrEmpty(ns))
        {
            sb.AppendLine($@"
namespace {ns}
{{
");
        }

        var hasAssign = HasAssignComponent(sb, model, moduleState);
        var hasCreate = HasCreate(sb, model, moduleState);
        var hasDispose = HasDispose(sb, model, moduleState);
        
        sb.AppendLine($@"
    struct {model.Source.Name};

    struct {model.Source.Name}GeneratedBase
    {{
");

        if (info.HasErrors)
        {
            foreach (var error in info.Errors)
            {
                sb.AppendLine($"""
        #error {error}
""");
            }

            sb.AppendLine();
        }

        sb.AppendLine("""
    public:
""");

        foreach (var member in members)
        {
            EmitDirtyMethods(sb, member, model, moduleState);
            EmitAccessorMethods(sb, member, model, moduleState, true);
        }

        EmitDirtyMaskAccessMethods(sb, model);

        if (hasAssign)
        {
            EmitAssign(sb, model, moduleState);
        }

        if (hasCreate)
        {
            EmitCreate(sb, model, moduleState);
        }

        if (hasDispose)
        {
            EmitDispose(sb, model, moduleState);
        }
        
        sb.AppendLine("""
    protected:
""");

        EmitDirtyMask(sb, model);

        foreach (var member in members)
        {
            EmitBackingField(sb, member, model, moduleState);
        }

        sb.AppendLine("""
    private:
""");

        foreach (var member in members)
        {
            EmitAccessorMethods(sb, member, model, moduleState, false);
        }

        sb.AppendLine("""
    protected:
""");
        if (info.EmitBindDelete)
        {
            EmitNativeEntityDeleteImpl(sb, model);
        }
        
        sb.AppendLine($@"
    }}; // struct {model.Source.Name}GeneratedBase
");
        if (!string.IsNullOrEmpty(ns))
        {
            sb.AppendLine($@"
}} // namespace {ns}
");
        }

        // NOTE: Emitting beginning of the file here so that all usings added during generation are added.
        var includesSb = new StringBuilder();
        includesSb.AppendLine("""
// <auto-generated/>
#pragma once

""");
        
        EmitIncludes(includesSb, moduleState);

        sb.Insert(0, includesSb.ToString());

        return sb.ToString();
    }
    
    private void AddDefaultIncludes(CppModuleState moduleState, DeriveTargetModel model)
    {
        moduleState.AddIncludeList([
            ("Interop/Vector3.h", false),
            ("Interop/Vector4.h", false),
            ("Native/Container/NativeString.h", false),
            ("Native/Container/NativeStringHash.h", false),
            ("Native/Container/NativeDictionary.h", false),
            ("Native/Container/IntHash.h", false),
            ("Native/Container/NativeList.h", false),
            ("ECS/Values/RawEntity.h", false),
        ]);

        foreach (var member in model.Members)
        {
            AddDefaultIncludes(moduleState, member);
        }
    }
    
    private void AddDefaultIncludes(CppModuleState moduleState, DeriveMemberModel model)
    {
        var attrIncludes = AttributeUtils.GetArrayAttribute<string>(model.Source.Symbol, "CppNativeFieldTypeAttribute", "includes");
        if (attrIncludes != null)
        {
            foreach (var path in attrIncludes)
            {
                moduleState.AddInclude(path, false);
            }
        }

        var includes = CppPaths(model.Source.Type);
        foreach (var path in includes)
        {
            moduleState.AddInclude(path, false);
        }
    }

    private void EmitIncludes(StringBuilder sb, CppModuleState moduleState)
    {
        var includes = moduleState.Includes.Distinct().ToList();
        includes.Sort();
        
        foreach (var ns in includes)
        {
            if (ns.AngleBrackets)
                sb.AppendLine($"""
#include <{ns.Path}>
""");
            else
                sb.AppendLine($"""
#include "{ns.Path}"
""");
        }
    }
    
    private void EmitDirtyMethods(
        StringBuilder sb,
        DeriveMemberModel member,
        DeriveTargetModel model,
        CppModuleState moduleState)
    {
        if (model.MaskInfo == null)
            return;

        var impl = GetEmitFieldSupportImpl(member, true);
        var context = CreateEmitContext(sb, member, model, moduleState);
        
        context.State.ResetIndent("        ");
        impl.EmitDirtyMethods(member.Source.Type, context);
    }
    
    private void EmitAccessorMethods(
        StringBuilder sb,
        DeriveMemberModel member,
        DeriveTargetModel model,
        CppModuleState moduleState,
        bool emitPublic)
    {
        var impl = GetEmitFieldSupportImpl(member, true);
        var context = CreateEmitContext(sb, member, model, moduleState);

        context.State.ResetIndent("        ");
        impl.EmitAccessorMethods(member.Source.Type, context, emitPublic);
    }
    
    private void EmitAssign(
        StringBuilder sb,
        DeriveTargetModel model,
        CppModuleState moduleState)
    {
        sb.Append($@"
        void Assign(const {CppTypeName(model.Source.Symbol)}GeneratedBase& value)
");
        sb.AppendLine("""
        {
""");
        
        foreach (var member in model.Members)
        {
            var impl = GetEmitFieldSupportImpl(member, true);
            var context = CreateEmitContext(sb, member, model, moduleState);
    
            context.State.ResetIndent("            ");
            impl.EmitAssignComponentBody(member.Source.Type, context);
        }
            
        sb.AppendLine("""
        }
""");
    }
    
    private bool HasAssignComponent(
        StringBuilder sb,
        DeriveTargetModel model,
        CppModuleState moduleState)
    {
        var result = true;
        foreach (var member in model.Members)
        {
            var impl = GetEmitFieldSupportImpl(member, true);
            var context = CreateEmitContext(sb, member, model, moduleState);
        
            result = result && impl.HasAssignComponent(member.Source.Type, context);
        }

        return result;
    }

    private bool HasCreate(
        StringBuilder sb,
        DeriveTargetModel model,
        CppModuleState moduleState)
    {
        var result = false;
        foreach (var member in model.Members)
        {
            var impl = GetEmitFieldSupportImpl(member, true);
            var context = CreateEmitContext(sb, member, model, moduleState);
        
            result = result || impl.HasCreate(member.Source.Type, context);
        }

        return result;
    }

    private void EmitCreate(
        StringBuilder sb,
        DeriveTargetModel model,
        CppModuleState moduleState)
    {
            sb.AppendLine("""
        void TryCreate(Yooni::Native::LowLevel::AllocatorKind allocatorKind)
        {
""");
        
            foreach (var member in model.Members)
            {
                var impl = GetEmitFieldSupportImpl(member, true);
                var context = CreateEmitContext(sb, member, model, moduleState);
        
                context.State.ResetIndent("            ");
                impl.EmitTryCreateBody(member.Source.Type, context);
            }
            
            sb.AppendLine("""
        }

""");
    }

    private bool HasDispose(
        StringBuilder sb,
        DeriveTargetModel model,
        CppModuleState moduleState)
    {
        var result = false;
        foreach (var member in model.Members)
        {
            var impl = GetEmitFieldSupportImpl(member, true);
            var context = CreateEmitContext(sb, member, model, moduleState);
        
            result = result || impl.HasDispose(member.Source.Type, context);
        }

        return result;
    }

    private void EmitDispose(
        StringBuilder sb,
        DeriveTargetModel model,
        CppModuleState moduleState)
    {
            sb.AppendLine("""
        void Dispose()
        {
""");
        
            foreach (var member in model.Members)
            {
                var impl = GetEmitFieldSupportImpl(member, true);
                var context = CreateEmitContext(sb, member, model, moduleState);
        
                context.State.ResetIndent("            ");
                impl.EmitDisposeBody(member.Source.Type, context);
            }
            
            sb.AppendLine("""
        }

""");
    }

    private void EmitDirtyMaskAccessMethods(StringBuilder sb, DeriveTargetModel model)
    {
        if (model.MaskInfo == null)
            return;
        
        sb.AppendLine("""
    public:
        void ClearDirty()
        {
            _dirtyMask = 0;
        }
        
        bool IsDirty()
        {
            return _dirtyMask != 0;
        }
        
""");
    }

    private void EmitDirtyMask(StringBuilder sb, DeriveTargetModel model)
    {
        var maskInfo = model.MaskInfo;
        if (maskInfo == null)
            return;
        
        if (model.Source.EmitDirtyMask)
        {
            sb.AppendLine($"""
        {CppTypeName(maskInfo.Type)} _dirtyMask = 0;

""");
        }
        else
        {
            sb.AppendLine($"""
        {CppTypeName(maskInfo.Type)} _dirtyMask = 0; // NOTE: Respecting the user-defined dirty mask size.

""");
        }
    }

    private void EmitBackingField(StringBuilder sb,
        DeriveMemberModel member,
        DeriveTargetModel model,
        CppModuleState moduleState)
    {
        if (member.Source.HasErrors)
        {
            foreach (var error in member.Source.Errors)
            {
                sb.AppendLine($"""
        #error {error}
""");
            }
        }
        
        var impl = GetEmitFieldSupportImpl(member, true);
        var context = CreateEmitContext(sb, member, model, moduleState);
        
        context.State.ResetIndent("        ");
        impl.EmitBackingField(member.Source.Type, context);
    }
    
    private void EmitNativeEntityDeleteImpl(StringBuilder sb, DeriveTargetModel model)
    {
        sb.AppendLine($$"""
        class NativeEntityDeleteImplGeneratedBase
        {
        public:
            struct NativeBinding
            {
                void* Target = nullptr;
                void (*OnEntityDeleteHandler)(
                    void* target,
                    Friflo::Engine::ECS::RawEntity entity,
                    {{model.Source.Name}}* comp) = nullptr;

                bool IsValid() const
                {
                    return Target != nullptr && OnEntityDeleteHandler != nullptr;
                }
            };
            
            NativeEntityDeleteImplGeneratedBase() = default;
            NativeEntityDeleteImplGeneratedBase(const NativeEntityDeleteImplGeneratedBase&) = delete;
            NativeEntityDeleteImplGeneratedBase(NativeEntityDeleteImplGeneratedBase&&) = delete;
            NativeEntityDeleteImplGeneratedBase& operator=(const NativeEntityDeleteImplGeneratedBase&) = delete;
            NativeEntityDeleteImplGeneratedBase& operator=(NativeEntityDeleteImplGeneratedBase&&) = delete;
            virtual ~NativeEntityDeleteImplGeneratedBase() = default;
            
            NativeBinding GetNativeBinding()
            {
                return NativeBinding
                {
                    .Target = this,
                    .OnEntityDeleteHandler = [](
                        void* target,
                        Friflo::Engine::ECS::RawEntity entity,
                        {{model.Source.Name}}* comp)
                    {
                        static_cast<NativeEntityDeleteImplGeneratedBase*>(target)->HandleEntityDelete(entity, *comp);
                    }
                };
            }

        protected:
            virtual void HandleEntityDelete(Friflo::Engine::ECS::RawEntity entity, {{model.Source.Name}}& comp) = 0;
        };
""");
    }
    
    private static bool HasGetEmitFieldSupportImpl(DeriveMemberModel member, bool fallback)
        => CppFieldSupportRegistry.FieldTypeSupportVisitor.TryGetImpl(member, fallback, out _);

    private static ICppFieldTypeSupportImpl GetEmitFieldSupportImpl(DeriveMemberModel member, bool fallback)
        => CppFieldSupportRegistry.FieldTypeSupportVisitor.GetImpl(member, fallback);
    
    private static CppEmitFieldSupportContext CreateEmitContext(StringBuilder sb, DeriveMemberModel member, DeriveTargetModel model, CppModuleState moduleState)
        => CppFieldSupportRegistry.CreateEmitFieldSupportContext(sb, member, model, moduleState);
}