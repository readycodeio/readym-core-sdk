using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadyM.Api.Generators.Archetypes;

/// <summary>
/// Completes an <c>[Archetype]</c> struct: the component its own accessors live in, everything its
/// includes contribute, and the conversions between it and the archetypes it includes.
/// </summary>
[Generator]
internal class ArchetypeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var archetypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            ArchetypeNames.ArchetypeAttribute,
            static (node, _) => node is StructDeclarationSyntax,
            Emit);

        context.RegisterSourceOutput(archetypes, static (spc, generated) =>
        {
            if (generated is not null)
                spc.AddSource(generated.Value.HintName, generated.Value.Source);
        });
    }

    private static (string HintName, string Source)? Emit(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol symbol || symbol.ContainingType is not null)
            return null;

        var model = DeclarationModel.For(symbol);
        var writer = new SourceWriter();

        HandleEmitter.File(writer, model);

        if (model.HasOwnComponent)
        {
            ComponentEmitter.Emit(writer, model.Component, model.Accessors);
            writer.Line();
        }

        AccessorEmitter.Emit(writer, model, HandleEmitter.ComponentSet(model));
        writer.Line();

        using (writer.Braces($"{model.Header} : {ArchetypeNames.Archetype}"))
        {
            HandleEmitter.Handle(writer, model);
            EmitIdentity(writer);
            HandleEmitter.Accessors(writer, model.Accessors, model.QualifiedAccessors, partial: true);
            EmitIncluded(writer, model);
            EmitOptional(writer, model);
            EmitConversions(writer, model);
        }

        return ($"{symbol.Name}.Archetype.g.cs", writer.ToString());
    }

    private static void EmitIdentity(SourceWriter writer)
    {
        writer.Line();
        writer.Line("public bool IsValid => _handle.IsAlive();");
        writer.Line();
        writer.Line($"public bool Has<T>() where T : struct, {ArchetypeNames.Tag} => _handle.HasTag<T>();");
        writer.Line();
        writer.Line($"public void Set<T>(bool set) where T : struct, {ArchetypeNames.Tag} => _handle.SetTag<T>(set);");
        writer.Line();
        writer.Line($"public bool Is<T>() where T : struct, {ArchetypeNames.Archetype} => _handle.Is<T>();");
        writer.Line();
        writer.Line($"public bool TryAs<T>(out T archetype) where T : struct, {ArchetypeNames.Archetype}");
        writer.Line("    => _handle.TryAs(out archetype);");
    }

    /// <summary>Accessors of everything included, flattened onto the archetype.</summary>
    private static void EmitIncluded(SourceWriter writer, DeclarationModel model)
    {
        foreach (var (include, accessor) in model.FlattenedAccessors())
        {
            writer.Line();

            if (!include.Optional)
            {
                HandleEmitter.Accessor(writer, accessor, include.Accessors, partial: false);
                continue;
            }

            // An absent optional mixin reads as null rather than throwing. Writing through it needs
            // the narrower handle below, which is what keeps a plain assignment from ever failing.
            writer.Line($"public {accessor.Type}? {accessor.Name}");
            writer.Line($"    => {include.Accessors}.TryGet{accessor.Name}(_handle, out var value) ? value : null;");
        }
    }

    private static void EmitOptional(SourceWriter writer, DeclarationModel model)
    {
        foreach (var include in model.Includes.Where(i => i is { Optional: true, Kind: IncludeKind.Mixin }))
        {
            var mixin = include.TypeName;
            var name = include.Type.Name;

            writer.Line();

            using (writer.Braces($"public bool {ArchetypeNames.TryGetOf(include.Type)}(out {mixin} {include.Parameter})"))
            {
                using (writer.Braces($"if ({include.Accessors}.Has(_handle))"))
                {
                    writer.Line($"{include.Parameter} = new {mixin}(_handle);");
                    writer.Line("return true;");
                }

                writer.Line();
                writer.Line($"{include.Parameter} = default;");
                writer.Line("return false;");
            }

            writer.Line();

            using (writer.Braces($"public {mixin} {ArchetypeNames.RequireOf(include.Type)}()"))
            {
                writer.Line($"if (!{include.Accessors}.Has(_handle))");
                writer.Line($"    throw new global::System.InvalidOperationException($\"{{_handle}} does not carry {name}.\");");
                writer.Line();
                writer.Line($"return new {mixin}(_handle);");
            }

            writer.Line();

            using (writer.Braces($"public {mixin} {ArchetypeNames.EnsureOf(include.Type)}()"))
            {
                writer.Line($"if (!{include.Accessors}.Has(_handle))");
                writer.Line($"    {include.Accessors}.Add(_handle);");
                writer.Line();
                writer.Line($"return new {mixin}(_handle);");
            }
        }
    }

    /// <summary>
    /// Including an archetype gives a conversion up, always allowed, and a checked one back down.
    /// </summary>
    private static void EmitConversions(SourceWriter writer, DeclarationModel model)
    {
        foreach (var include in model.Includes.Where(i => i.Kind == IncludeKind.Archetype))
        {
            writer.Line();
            writer.Line($"public static implicit operator {include.TypeName}({model.QualifiedName} value)");
            writer.Line("    => new(value._handle);");
            writer.Line();

            using (writer.Braces($"public static explicit operator {model.QualifiedName}({include.TypeName} value)"))
            {
                writer.Line($"var handle = {ArchetypeNames.EntityHandle}.Of(value);");
                writer.Line();
                writer.Line($"if (!handle.Is<{model.QualifiedName}>())");
                writer.Line($"    throw new global::System.InvalidCastException($\"{{handle}} is not a {model.Name}.\");");
                writer.Line();
                writer.Line($"return new {model.QualifiedName}(handle);");
            }
        }
    }
}
