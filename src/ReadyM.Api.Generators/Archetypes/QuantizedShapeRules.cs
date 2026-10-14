using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Archetypes;

/// [Quantize], [QuantizeAngle] and [HalfPrecision] on a shape's property only mean something when the shape's own
/// component is replicated.
internal static class QuantizedShapeRules
{
    private static readonly string[] Attributes = ["QuantizeAttribute", "QuantizeAngleAttribute", "HalfPrecisionAttribute"];

    public static readonly DiagnosticDescriptor OnExplicitComponent = new(
        "READYM039",
        "Quantization is stated on a borrowed component",
        "'{0}.{1}' is backed by '{2}', which decides for itself how its fields go over the wire. Mark the "
        + "component's field with [{3}] instead.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotReplicated = new(
        "READYM040",
        "Quantization is stated on a shape nothing sends",
        "'{0}.{1}' is marked [{2}], but '{0}' is not replicated, so nothing goes over the wire. Drop [{2}], or "
        + "replicate the shape.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static ImmutableArray<Diagnostic> Check(DeclarationModel model)
    {
        var found = new List<Diagnostic>();

        foreach (var property in model.Symbol.GetMembers().OfType<IPropertySymbol>())
        {
            var attribute = Attributes.FirstOrDefault(name => AttributeUtils.HasAttribute(property, name));
            if (attribute is null)
                continue;

            var stated = attribute.Substring(0, attribute.Length - "Attribute".Length);
            var at = property.Locations.FirstOrDefault() ?? Location.None;

            if (model.ExplicitComponent is { } component)
                found.Add(Diagnostic.Create(OnExplicitComponent, at, model.Name, property.Name, component.ToDisplayString(), stated));
            else if (!model.IsReplicated)
                found.Add(Diagnostic.Create(NotReplicated, at, model.Name, property.Name, stated));
        }

        return [.. found];
    }
}
