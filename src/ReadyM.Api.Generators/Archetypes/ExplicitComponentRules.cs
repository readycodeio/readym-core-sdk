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

    private const string Component = "global::Friflo.Engine.ECS.IComponent";
    private const string Link = "global::Friflo.Engine.ECS.ILinkComponent";

    public static ImmutableArray<Diagnostic> Check(DeclarationModel model, Compilation compilation)
    {
        var component = model.ExplicitComponent;

        if (component is null)
            return [];

        var found = new List<Diagnostic>();
        var at = model.Symbol.Locations.FirstOrDefault() ?? Location.None;
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

    private static bool Implements(INamedTypeSymbol type, string qualified)
        => type.AllInterfaces.Any(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == qualified);
}
