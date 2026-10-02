using System.Text;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Archetypes;

namespace ReadyM.Api.Generators;

/// <summary>
/// The registration that puts an RPC class in DI, which both sides emit the same way. A mod names
/// only its contracts, the way a <c>[Service]</c> names nothing at all.
/// </summary>
internal static class RpcRegistrationEmitter
{
    public static void Emit(
        StringBuilder sb, INamedTypeSymbol classSymbol, bool moduleInitializer, string side)
    {
        var name = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        sb.AppendLine();
        sb.AppendLine("    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        sb.AppendLine("    public static class Registration");
        sb.AppendLine("    {");

        // A module initializer runs this where the target has one, and the mod loader runs it for
        // every assembly it loads, so it has to be safe to say twice.
        sb.AppendLine("        private static bool _registered;");
        sb.AppendLine();

        if (moduleInitializer)
            sb.AppendLine("        [global::System.Runtime.CompilerServices.ModuleInitializer]");

        sb.AppendLine("        public static void Register()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_registered)");
        sb.AppendLine("                return;");
        sb.AppendLine();
        sb.AppendLine("            _registered = true;");
        sb.AppendLine();
        sb.AppendLine(
            $"            {ArchetypeNames.RpcHandlerRegistry}.Declare<{name}>({ArchetypeNames.RpcSide}.{side});");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }
}
