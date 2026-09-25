using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ReadyM.Api.Generators.Archetypes;

/// Checks a shape whose storage already exists, before anything is emitted against it.
internal static class ExplicitComponentRules
{
    public static readonly DiagnosticDescriptor NotAComponent = new(
        "READYM004",
        "Explicit component is not usable",
        "'{0}' cannot back a shape: {1}",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoSuchMember = new(
        "READYM005",
        "Explicit component has no matching member",
        "'{0}' has no field or property named '{1}'. Name one with [ExplicitMember].",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotAssignable = new(
        "READYM006",
        "Explicit member cannot be written",
        "'{0}.{1}' cannot be assigned, so '{2}' must be declared without a setter.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotVisible = new(
        "READYM007",
        "Explicit component is not visible",
        "'{0}' is not visible from this assembly. Grant it InternalsVisibleTo.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NothingToForward = new(
        "READYM008",
        "Explicit collection matches nothing",
        "Nothing matches the collection '{0}': {1}",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor WrongType = new(
        "READYM012",
        "Explicit member has an unrelated type",
        "'{0}.{1}' holds '{2}', which cannot be read as '{3}'. Declare the value as '{2}', or as a type derived from it.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnregisteredInstance = new(
        "READYM013",
        "Generic component instantiation is not registered",
        "'{0}' is not registered with the ECS, so an entity carrying it cannot be built. Add "
        + "[assembly: GlobalGenericInstanceType(typeof({1}), \"<key>\", {2})], or name an instantiation "
        + "that is registered.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private const string Component = "global::Friflo.Engine.ECS.IComponent";
    private const string Link = "global::Friflo.Engine.ECS.ILinkComponent";
    private const string GenericInstance = "global::Friflo.Engine.ECS.GenericInstanceTypeAttribute";
    private const string GlobalGenericInstance = "global::Friflo.Engine.ECS.GlobalGenericInstanceTypeAttribute";

    /// The generic type as an author writes it in typeof, with its parameters left off.
    private static readonly SymbolDisplayFormat Unbound = SymbolDisplayFormat.FullyQualifiedFormat
        .WithGenericsOptions(SymbolDisplayGenericsOptions.None);

    public static ImmutableArray<Diagnostic> Check(DeclarationModel model, Compilation compilation)
    {
        var found = new List<Diagnostic>();
        var at = model.Symbol.Locations.FirstOrDefault() ?? Location.None;
        var component = model.ExplicitComponent;

        CheckCollections(model, component, found, at);

        if (component is null)
            return [.. found];

        var name = component.ToDisplayString();

        if (!compilation.IsSymbolAccessibleWithin(component, compilation.Assembly))
            found.Add(Diagnostic.Create(NotVisible, at, name));

        if (component.TypeKind != TypeKind.Struct || !Implements(component, Component))
            found.Add(Diagnostic.Create(NotAComponent, at, name, "it is not a struct implementing IComponent."));
        else if (Implements(component, Link))
            found.Add(Diagnostic.Create(NotAComponent, at, name,
                "link components carry a relationship the SDK cannot maintain through an accessor yet."));
        else if (!Registered(compilation, component))
            found.Add(Diagnostic.Create(UnregisteredInstance, at, name,
                component.ConstructedFrom.ToDisplayString(Unbound),
                string.Join(", ", component.TypeArguments.Select(argument => $"typeof({argument.ToDisplayString()})"))));

        foreach (var accessor in model.Accessors)
        {
            if (accessor.Declared is not { } declared)
                continue;

            var where = declared.Locations.FirstOrDefault() ?? at;
            var target = AccessorModel.Target(component, declared);

            if (target is null)
            {
                found.Add(Diagnostic.Create(NoSuchMember, where, name, accessor.Name));
                continue;
            }

            if (declared.SetMethod is not null && !AccessorModel.Assignable(target))
                found.Add(Diagnostic.Create(NotAssignable, where, name, target.Name, accessor.Name));

            if (AccessorModel.TypeOf(target) is { } held && !Readable(compilation, held, declared, target))
                found.Add(Diagnostic.Create(WrongType, where, name, target.Name,
                    held.ToDisplayString(), declared.Type.ToDisplayString()));
        }

        return [.. found];
    }

    /// <summary>
    /// A name that forwards nothing is worth reporting: the shape still compiles, and the members the
    /// author expected are simply absent, which only shows up where they are called.
    /// </summary>
    private static void CheckCollections(
        DeclarationModel model,
        INamedTypeSymbol? component,
        List<Diagnostic> found,
        Location at)
    {
        foreach (var collection in model.Collections)
        {
            var where = DeclarationModel.LocationOfCollection(model.Symbol, collection, at);

            if (component is null)
            {
                found.Add(Diagnostic.Create(NothingToForward, where, collection,
                    "the shape has no [ExplicitComponent] to forward from."));
                continue;
            }

            if (component.GetMembers().Any(member => DeclarationModel.Forwardable(member, collection)))
                continue;

            found.Add(Diagnostic.Create(NothingToForward, where, collection,
                $"'{component.ToDisplayString()}' has no public member naming it."));
        }
    }

    /// Whether the ECS knows this instantiation of a generic component. Each closed generic is a
    /// component of its own, and the schema only holds the ones an attribute names.
    private static bool Registered(Compilation compilation, INamedTypeSymbol component)
    {
        if (!component.IsGenericType || component.IsUnboundGenericType)
            return true;

        var definition = component.OriginalDefinition;
        var arguments = component.TypeArguments;

        // Named on the generic type itself, which only its own assembly can do.
        foreach (var attribute in definition.GetAttributes())
            if (Named(attribute, GenericInstance) && Arguments(attribute, 1, arguments))
                return true;

        // Or named by any assembly in sight. The schema reads every assembly that is loaded, which
        // here is every one this compilation references.
        foreach (var assembly in Assemblies(compilation))
            foreach (var attribute in assembly.GetAttributes())
                if (Named(attribute, GlobalGenericInstance)
                    && attribute.ConstructorArguments.Length > 2
                    && Same(attribute.ConstructorArguments[0].Value as INamedTypeSymbol, definition)
                    && Arguments(attribute, 2, arguments))
                    return true;

        return false;
    }

    private static IEnumerable<IAssemblySymbol> Assemblies(Compilation compilation)
        => new[] { compilation.Assembly }.Concat(compilation.SourceModule.ReferencedAssemblySymbols);

    private static bool Named(AttributeData attribute, string qualified)
        => attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == qualified;

    /// Whether the types an attribute names, after its leading arguments, are the ones asked for.
    private static bool Arguments(AttributeData attribute, int skip, ImmutableArray<ITypeSymbol> wanted)
    {
        var named = attribute.ConstructorArguments.Skip(skip).ToList();

        return named.Count == wanted.Length
               && named.Select((argument, i) => Same(argument.Value as ITypeSymbol, wanted[i])).All(same => same);
    }

    private static bool Same(ITypeSymbol? left, ITypeSymbol? right)
        => left is not null && right is not null
           && SymbolEqualityComparer.Default.Equals(
               left.OriginalDefinition.WithNullableAnnotation(NullableAnnotation.None),
               right.OriginalDefinition.WithNullableAnnotation(NullableAnnotation.None));

    /// Whether the component's own value can reach the shape: as it stands, or narrowed to the
    /// type the shape declared.
    private static bool Readable(Compilation compilation, ITypeSymbol held, IPropertySymbol declared, ISymbol target)
        => compilation is not CSharpCompilation csharp
           || csharp.ClassifyConversion(held, declared.Type).IsImplicit
           || AccessorModel.NarrowsFrom(declared, target);

    private static bool Implements(INamedTypeSymbol type, string qualified)
        => type.AllInterfaces.Any(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == qualified);
}
