using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

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

    private const string Component = "global::Friflo.Engine.ECS.IComponent";
    private const string Link = "global::Friflo.Engine.ECS.ILinkComponent";

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

    private static bool Implements(INamedTypeSymbol type, string qualified)
        => type.AllInterfaces.Any(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == qualified);
}
