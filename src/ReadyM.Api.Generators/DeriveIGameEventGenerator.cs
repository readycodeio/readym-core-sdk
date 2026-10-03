using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReadyM.Api.Generators.Derive.GameEvents;

namespace ReadyM.Api.Generators;

/// <summary>
/// Generates the <c>IGameEvent</c> methods of an event marked <c>[DeriveIGameEvent]</c>, with the policy its
/// discriminator attribute chooses (<c>[OwnershipBased]</c> and the rest of <see cref="GameEventSupportRegistry"/>),
/// and the <c>IGameEventRequiresContext</c> markers for what those methods read. An event without
/// <c>[DeriveIGameEvent]</c> writes the methods and the markers by hand. Every event with markers gets its
/// <c>AcceptContexts</c>, and every assembly with events gets one <c>GameEventRegistration</c> listing them.
/// </summary>
[Generator]
internal sealed class DeriveIGameEventGenerator : IIncrementalGenerator
{
    private const string AttributeNamespace = "ReadyM.Api.Mapping.Events";
    private const string DeriveAttributeName = "DeriveIGameEventAttribute";
    private const string RequiresContextName = "IGameEventRequiresContext";
    private const string GameEventName = "IGameEvent";
    private const string RegistrationMetadataName = "ReadyM.Api.ECS.Registry.IAllTypeRegistration";
    private const string EntityType = "Friflo.Engine.ECS.Entity";
    private const string RawEntityType = "Friflo.Engine.ECS.RawEntity";
    private const string Events = "global::ReadyM.Api.Mapping.Events";

    /// <summary>What one struct contributes: the part that emits, and whether the assembly's registration lists it.</summary>
    private sealed class EventInfo(string fullName, GameEventModel? derived, IReadOnlyList<string> declaredRequirements, bool emits, bool registered, string? error)
    {
        public string FullName { get; } = fullName;
        public GameEventModel? Derived { get; } = derived;
        public IReadOnlyList<string> DeclaredRequirements { get; } = declaredRequirements;
        public bool Emits { get; } = emits;
        public bool Registered { get; } = registered;
        public string? Error { get; } = error;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var infos = context.SyntaxProvider
            .CreateSyntaxProvider(Predicate, Transform)
            .Where(i => i is not null);

        context.RegisterSourceOutput(infos.Where(i => i!.Emits), (spc, info) =>
        {
            var name = info!.FullName.Replace("global::", "");
            spc.AddSource($"{name}.GameEvent.g.cs", Emit(info));
        });

        var registration = infos.Where(i => i!.Registered).Select((i, _) => i!.FullName).Collect()
            .Combine(context.CompilationProvider);
        context.RegisterSourceOutput(registration, (spc, pair) =>
        {
            var source = EmitRegistration(pair.Left, pair.Right);
            if (source != null)
                spc.AddSource("GameEventRegistration.g.cs", source);
        });
    }

    private static bool Predicate(SyntaxNode node, CancellationToken _)
        => node is StructDeclarationSyntax { AttributeLists.Count: > 0 } or StructDeclarationSyntax { BaseList: not null };

    private static bool IsDerive(AttributeData attribute)
        => attribute.AttributeClass is { } type
           && type.ContainingNamespace.ToDisplayString() == AttributeNamespace
           && type.Name == DeriveAttributeName;

    private static bool IsDiscriminator(AttributeData attribute)
        => attribute.AttributeClass is { } type
           && type.ContainingNamespace.ToDisplayString() == AttributeNamespace
           && GameEventSupportRegistry.DiscriminatorNames.Contains(type.Name);

    private static bool IsEventsInterface(INamedTypeSymbol type, string name)
        => type.Name == name && type.ContainingNamespace.ToDisplayString() == AttributeNamespace;

    private static EventInfo? Transform(GeneratorSyntaxContext context, CancellationToken ct)
    {
        var node = (StructDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(node, ct) is not INamedTypeSymbol symbol)
            return null;

        // NOTE: Each part of a partial struct is its own syntax node, and each would emit. Only the first part in
        // declaration order emits and registers, so the output and any error appear once.
        if (symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(ct) != node)
            return null;

        var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var declared = symbol.Interfaces
            .Where(i => i.IsGenericType && IsEventsInterface(i, RequiresContextName))
            .Select(i => i.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
            .ToArray();

        var attributes = symbol.GetAttributes();
        var derive = attributes.FirstOrDefault(IsDerive);
        var discriminators = attributes.Where(IsDiscriminator).ToArray();
        var topLevel = symbol.ContainingType == null;

        if (derive == null && discriminators.Length == 0)
        {
            var handWritten = symbol.AllInterfaces.Any(i => IsEventsInterface(i, GameEventName));
            if (!handWritten && declared.Length == 0)
                return null;

            string? error = null;
            if (declared.Length > 0 && !topLevel)
                error = $"{symbol.ToDisplayString()} is nested in {symbol.ContainingType!.ToDisplayString()}; an event that requires contexts must be a top-level struct.";
            else if (declared.Length > 0 && !node.Modifiers.Any(m => m.Text == "partial"))
                error = $"{symbol.ToDisplayString()} requires contexts but is not partial; its AcceptContexts is generated.";

            return new EventInfo(fullName, null, declared, emits: declared.Length > 0, registered: handWritten && topLevel, error);
        }

        var model = TransformDerived(symbol, derive, discriminators, ct);
        return new EventInfo(fullName, model, declared, emits: true, registered: topLevel && model.Errors.Count == 0, null);
    }

    private static GameEventModel TransformDerived(INamedTypeSymbol symbol, AttributeData? derive, AttributeData[] discriminators, CancellationToken ct)
    {
        var errors = new List<string>();
        var display = symbol.ToDisplayString();

        if (derive == null)
        {
            var name = discriminators[0].AttributeClass!.Name;
            errors.Add($"{display} has a discriminator ({name}) but no [DeriveIGameEvent]; a discriminator only chooses the policy [DeriveIGameEvent] generates.");
            return new GameEventModel(symbol, discriminators[0], null, errors);
        }

        if (discriminators.Length != 1)
        {
            var names = discriminators.Length == 0 ? "none" : string.Join(", ", discriminators.Select(d => d.AttributeClass!.Name));
            errors.Add($"{display} has [DeriveIGameEvent] and {discriminators.Length} discriminators ({names}); a generated game event takes exactly one discriminator.");
            if (discriminators.Length == 0)
                return new GameEventModel(symbol, derive, null, errors);
        }

        if (symbol.ContainingType != null)
            errors.Add($"{display} is nested in {symbol.ContainingType.ToDisplayString()}; a game event must be a top-level struct.");

        var discriminator = discriminators[0];
        IFieldSymbol? subject = null;
        if (GameEventSupportRegistry.NamesNeedingSubject.Contains(discriminator.AttributeClass!.Name))
            subject = ResolveSubject(symbol, discriminator, display, errors);

        return new GameEventModel(symbol, discriminator, subject, errors);
    }

    private static IFieldSymbol? ResolveSubject(INamedTypeSymbol symbol, AttributeData discriminator, string display, List<string> errors)
    {
        var name = discriminator.ConstructorArguments.Length == 1 ? discriminator.ConstructorArguments[0].Value as string : null;
        if (string.IsNullOrEmpty(name))
        {
            errors.Add($"{display}: [{discriminator.AttributeClass!.Name}] needs the subject field's name, written with nameof.");
            return null;
        }

        var field = symbol.GetMembers(name!).OfType<IFieldSymbol>().FirstOrDefault(f => !f.IsStatic);
        if (field == null)
        {
            errors.Add($"{display}: the subject '{name}' is not an instance field of the event.");
            return null;
        }

        var type = field.Type.ToDisplayString();
        if (type != EntityType && type != RawEntityType)
        {
            errors.Add($"{display}: the subject '{name}' is a {type}; a subject must be an Entity or RawEntity.");
            return null;
        }

        return field;
    }

    private static string Emit(EventInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");

        var errors = new List<string>(info.Derived?.Errors ?? System.Array.Empty<string>());
        if (info.Error != null)
            errors.Add(info.Error);
        if (errors.Count > 0)
        {
            foreach (var error in errors)
                sb.AppendLine($"#error Game event {error}");
            return sb.ToString();
        }

        var model = info.Derived;
        var symbol = model?.Event;
        var impl = model != null ? GameEventSupportRegistry.SupportVisitor.GetImpl(model, fallback: false) : null;
        var generated = impl?.RequiredContexts ?? System.Array.Empty<string>();
        var requirements = generated.Concat(info.DeclaredRequirements).Distinct().ToArray();

        var ns = info.FullName.Replace("global::", "");
        var dot = ns.LastIndexOf('.');
        var typeName = dot < 0 ? ns : ns.Substring(dot + 1);
        if (dot >= 0)
            sb.AppendLine($"namespace {ns.Substring(0, dot)};").AppendLine();

        var readOnly = symbol?.IsReadOnly == true ? "readonly " : "";
        var bases = new List<string>();
        if (impl != null)
            bases.Add($"{Events}.IGameEvent");
        bases.AddRange(generated.Select(r => $"{Events}.IGameEventRequiresContext<{r}>"));

        sb.AppendLine(bases.Count > 0 ? $"{readOnly}partial struct {typeName} : {string.Join(", ", bases)}" : $"{readOnly}partial struct {typeName}");
        sb.AppendLine("{");

        if (impl != null)
        {
            var context = new CSharpEmitGameEventContext(sb, model!);
            const string parameter = $"{Events}.GameEventContextRegistry {CSharpEmitGameEventContext.ContextsParameter}";

            sb.AppendLine($"    public {Events}.GameEventNotifyResult CanGameEventNotifyEcs({parameter})");
            sb.Append("        => ");
            impl.EmitCanGameEventNotifyEcsBody(context);
            sb.AppendLine(";").AppendLine();

            sb.AppendLine($"    public {Events}.GameEventResult CanGameEventRunLocally({parameter})");
            sb.Append("        => ");
            impl.EmitCanGameEventRunLocallyBody(context);
            sb.AppendLine(";").AppendLine();

            sb.AppendLine($"    public {Events}.GameEventResult CanEcsInvokeGameEvent({parameter})");
            sb.Append("        => ");
            impl.EmitCanEcsInvokeGameEventBody(context);
            sb.AppendLine(";");
        }

        if (requirements.Length > 0)
        {
            if (impl != null)
                sb.AppendLine();
            sb.AppendLine($"    void {Events}.IGameEventRequiresContextBase.AcceptContexts({Events}.IGameEventContextVisitor visitor)");
            sb.AppendLine("    {");
            foreach (var requirement in requirements)
                sb.AppendLine($"        visitor.Accept<{requirement}>();");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string? EmitRegistration(ImmutableArray<string> events, Compilation compilation)
    {
        if (events.IsEmpty)
            return null;

        // NOTE: The registration interface is internal to ReadyM.Api; an assembly that cannot see it registers nothing.
        var registration = compilation.GetTypeByMetadataName(RegistrationMetadataName);
        if (registration == null || !compilation.IsSymbolAccessibleWithin(registration, compilation.Assembly))
            return null;

        var ns = string.Join(".", (compilation.AssemblyName ?? "Events").Split('.').Select(Identifier));

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine($"namespace {ns};").AppendLine();
        sb.AppendLine("/// <summary>Registers every game event this assembly compiles.</summary>");
        sb.AppendLine("internal sealed class GameEventRegistration : global::ReadyM.Api.ECS.Registry.IAllTypeRegistration");
        sb.AppendLine("{");
        sb.AppendLine("    public void Register(global::ReadyM.Api.ECS.Registry.IAllTypeRegistry registry)");
        sb.AppendLine("    {");
        foreach (var name in events.Distinct().OrderBy(n => n, System.StringComparer.Ordinal))
            sb.AppendLine($"        registry.RegisterEvent<{name}>();");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string Identifier(string part)
    {
        var chars = part.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray();
        var id = new string(chars);
        return id.Length == 0 || char.IsDigit(id[0]) ? $"_{id}" : id;
    }
}
