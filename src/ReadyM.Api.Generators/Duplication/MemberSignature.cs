using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Duplication;

/// <summary>
/// Builds a comparable key for a member so a copied member can be matched against one the target already declares.
/// Keys are rendered with fully qualified type names, with references to the source type remapped to the target,
/// so a hand-written replacement in the target lines up with the source member it replaces.
/// </summary>
internal static class MemberSignature
{
    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    /// <summary>Signature key for a member, or <c>null</c> for members that cannot be redeclared (e.g. a static ctor).</summary>
    public static string? Create(ISymbol member, string sourceTypeName, string targetTypeName)
    {
        switch (member)
        {
            case IMethodSymbol method:
                return CreateMethodKey(method, sourceTypeName, targetTypeName);

            case IPropertySymbol { IsIndexer: true } indexer:
                return "I:" + RenderParameters(indexer.Parameters, sourceTypeName, targetTypeName);

            default:
                return "N:" + member.Name;
        }
    }

    private static string? CreateMethodKey(IMethodSymbol method, string sourceTypeName, string targetTypeName)
    {
        var name = method.MethodKind switch
        {
            MethodKind.Constructor => ".ctor",
            MethodKind.StaticConstructor => ".cctor",
            MethodKind.Conversion => method.Name + "~" + Remap(method.ReturnType.ToDisplayString(TypeFormat), sourceTypeName, targetTypeName),
            MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove => null,
            _ => method.Name
        };

        if (name is null)
            return null;

        return "M:" + name + "`" + method.Arity + RenderParameters(method.Parameters, sourceTypeName, targetTypeName);
    }

    private static string RenderParameters(
        IEnumerable<IParameterSymbol> parameters,
        string sourceTypeName,
        string targetTypeName)
    {
        var sb = new StringBuilder("(");
        var first = true;

        foreach (var parameter in parameters)
        {
            if (!first)
                sb.Append(',');

            first = false;

            if (parameter.RefKind != RefKind.None)
                sb.Append(parameter.RefKind.ToString().ToLowerInvariant()).Append(' ');

            sb.Append(Remap(parameter.Type.ToDisplayString(TypeFormat), sourceTypeName, targetTypeName));
        }

        return sb.Append(')').ToString();
    }

    private static string Remap(string displayString, string sourceTypeName, string targetTypeName)
        => sourceTypeName.Length == 0 ? displayString : displayString.Replace(sourceTypeName, targetTypeName);

    /// <summary>Collects the signature keys and member names the target declares by hand.</summary>
    public static (HashSet<string> Signatures, HashSet<string> Names) CollectDeclared(INamedTypeSymbol target)
    {
        var signatures = new HashSet<string>();
        var names = new HashSet<string>();

        foreach (var member in target.GetMembers())
        {
            if (member.IsImplicitlyDeclared)
                continue;

            // Property/event accessors are covered by the property or event itself.
            if (member is IMethodSymbol { AssociatedSymbol: not null })
                continue;

            names.Add(member.Name);

            var key = Create(member, sourceTypeName: string.Empty, targetTypeName: string.Empty);
            if (key is not null)
                signatures.Add(key);
        }

        return (signatures, names);
    }
}
