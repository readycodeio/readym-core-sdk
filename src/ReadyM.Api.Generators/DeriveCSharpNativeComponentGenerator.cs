using System;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadyM.Api.Generators;

[Generator]
internal sealed class DeriveCSharpNativeComponentGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var typeLevelProvider = context.SyntaxProvider.CreateSyntaxProvider(Predicate, TransformTypeLevel);
        var assemblyLevelProvider = context.SyntaxProvider.CreateSyntaxProvider(AssemblyPredicate, TransformAssemblyLevel);
        var codeProvider =
            typeLevelProvider
                .Collect()
                .Combine(assemblyLevelProvider.Collect())
                .SelectMany((result, _) => result.Left.Concat(result.Right))
                .Where(static x => x != null)
                .Select(Transform);

        context.RegisterSourceOutput(
            codeProvider,
            static (spc, nameAndContent) =>
            {
                if (string.IsNullOrEmpty(nameAndContent.Name))
                    return;
                
                spc.AddSource($"{nameAndContent.Name}.g.cs", nameAndContent.Code);
            });
    }

    private static bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        => DeriveDiscoverUtils.TypePredicate(
            syntaxNode,
            cancellationToken,
            "NativeComponentAttribute",
            "NativeComponentForAttribute");

    private static bool AssemblyPredicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        => DeriveDiscoverUtils.AssemblyPredicate(
            syntaxNode,
            cancellationToken,
            "NativeComponentAttribute",
            "NativeComponentForAttribute");

    private static DeriveDiscoverUtils.TargetCandidate? TransformTypeLevel(GeneratorSyntaxContext context, CancellationToken ct)
        => DeriveDiscoverUtils.TransformTypeLevel(
            context,
            ct,
            "NativeComponentAttribute");

    private static DeriveDiscoverUtils.TargetCandidate? TransformAssemblyLevel(GeneratorSyntaxContext context, CancellationToken ct)
        => DeriveDiscoverUtils.TransformAssemblyLevel(
            context,
            ct,
            "NativeComponentForAttribute",
            "skipCSharp");

    private (string Name, string Code) Transform(DeriveDiscoverUtils.TargetCandidate? candidate, CancellationToken ct)
    {
        if (ct.IsCancellationRequested || candidate == null)
            return (string.Empty, string.Empty);

        var symbol = candidate.Symbol;
        DeriveTargetModel targetModel;

        if (candidate.Context != null)
        {
            targetModel = DeriveComponentUtils.GetTargetModel(
                candidate.IsExternal,
                symbol,
                candidate.Context.Value);
        }
        else
        {
            targetModel = DeriveComponentUtils.GetTargetModel(
                candidate.IsExternal,
                symbol,
                candidate.Attribute,
                null);
        }
        
        if (!targetModel.Source.EmitBindDelete)
            return (string.Empty, string.Empty);
        
        var code = GenerateNativeBindingComponent(targetModel, candidate.Context);

        var genName = DeriveUtils.GetGeneratedFileName(symbol);

        return (genName, code);
    }
    
    private static string GetTypeParameterList(StructDeclarationSyntax structDecl)
    {
        return structDecl.TypeParameterList?.ToString() ?? string.Empty;
    }
    
    private static string GetTypeArgumentList(StructDeclarationSyntax structDecl)
    {
        if (structDecl.TypeParameterList is null)
            return string.Empty;

        var args = string.Join(
            ", ",
            structDecl.TypeParameterList.Parameters.Select(static p => p.Identifier.ToString()));

        return $"<{args}>";
    }
    
    private static string GetTypeParameterList(INamedTypeSymbol symbol)
    {
        if (symbol.TypeParameters.Length == 0)
            return string.Empty;

        var args = string.Join(
            ", ",
            symbol.TypeParameters.Select(static p => p.Name));

        return $"<{args}>";
    }
    
    private static string GetTypeArgumentList(INamedTypeSymbol symbol)
    {
        if (symbol.TypeArguments.Length == 0)
            return string.Empty;

        var args = string.Join(
            ", ",
            symbol.TypeArguments.Select(static p => p.ToDisplayString()));

        return $"<{args}>";
    }
    
    private string GenerateNativeBindingComponent(
        DeriveTargetModel model,
        GeneratorSyntaxContext? context)
    {
        var info = model.Source;
        var structDecl = context?.Node as StructDeclarationSyntax;

        var namedDecl = model.Source.Symbol as INamedTypeSymbol;
        var typeParameterList = structDecl != null
            ? GetTypeParameterList(structDecl)
            : namedDecl != null
                ? GetTypeArgumentList(namedDecl)
                : "";
        var typeArgumentList = structDecl != null
            ? GetTypeArgumentList(structDecl)
            : namedDecl != null
                ? GetTypeArgumentList(namedDecl)
                : "";
        var externalModule = info.IsExternal;

        var componentType = $"{info.Name}{typeArgumentList}";

        var access = model.Source.Symbol.DeclaredAccessibility.ToString().ToLower(); // public, internal, etc.
        var sb = new StringBuilder();

        sb.Append($@"// <auto-generated/>
#nullable enable

using System.Runtime.InteropServices;
using ReadyM.Api.ECS.Managers;
using Friflo.Engine.ECS;

namespace {info.Namespace};
");

        if (externalModule)
        {
            sb.Append($"""
{access} class {info.Name}Extensions
""");
        }
        else
        {
            sb.Append($"""
{access} partial struct {info.Name}{typeParameterList}
""");
        }
        
        sb.Append($@"
{{
    public unsafe class NativeEntityDeleteImpl : IEntityDeleteImpl
    {{
        [StructLayout(LayoutKind.Sequential)]
        public struct NativeBinding
        {{
            public void* Target;
            public delegate* unmanaged<void*, Friflo.Engine.ECS.RawEntity, {componentType}*, void> OnEntityDeleteHandler;

            public bool IsValid
                => Target != null && OnEntityDeleteHandler != null;

            public void EnsureValid()
                => EnsureValid($""{{nameof({info.Name}{typeParameterList})}}.{{nameof(NativeEntityDeleteImpl)}}"");

            public void EnsureValid(string path)
            {{
                if (Target == null)
                    throw new System.InvalidOperationException($""{{path}}.Target is null"");
                if (OnEntityDeleteHandler == null)
                    throw new System.InvalidOperationException($""{{path}}.OnEntityDeleteHandler is null"");
            }}
        }}

        private NativeBinding _binding;

        public void HandleDelete(Friflo.Engine.ECS.Entity entity)
        {{
            if (!_binding.IsValid)
            {{
                throw new System.InvalidOperationException(""{componentType}.NativeEntityDeleteImpl not bound"");
            }}

            if (!entity.TryGetComponent<{componentType}>(out var comp))
                return;

            _binding.OnEntityDeleteHandler(_binding.Target, entity.RawEntity, &comp);
        }}

        public void BindNative(NativeBinding binding)
        {{
            _binding = binding;
        }}
    }}
}}

#nullable disable
");

        return sb.ToString();
    }
}