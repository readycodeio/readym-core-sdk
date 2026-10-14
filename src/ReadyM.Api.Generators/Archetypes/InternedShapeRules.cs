using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Archetypes;

/// [Interned] on a shape's property only means something when the shape's own component is replicated.
internal static class InternedShapeRules
{
    public static readonly DiagnosticDescriptor OnExplicitComponent = new(
        "READYM037",
        "Interned is stated on a borrowed component",
        "'{0}.{1}' is backed by '{2}', which decides for itself how its fields go over the wire. Mark the "
        + "component's field with [Interned] instead.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotReplicated = new(
        "READYM038",
        "Interned is stated on a shape nothing sends",
        "'{0}.{1}' is marked [Interned], but '{0}' is not replicated, so nothing goes over the wire. Drop "
        + "[Interned], or replicate the shape.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static ImmutableArray<Diagnostic> Check(DeclarationModel model)
    {
        var found = new List<Diagnostic>();

        foreach (var property in model.Symbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (!AttributeUtils.HasAttribute(property, "InternedAttribute"))
                continue;

            var at = property.Locations.FirstOrDefault() ?? Location.None;

            if (model.ExplicitComponent is { } component)
                found.Add(Diagnostic.Create(OnExplicitComponent, at, model.Name, property.Name, component.ToDisplayString()));
            else if (!model.IsReplicated)
                found.Add(Diagnostic.Create(NotReplicated, at, model.Name, property.Name));
        }

        return [.. found];
    }
}
