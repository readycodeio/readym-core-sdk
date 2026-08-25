using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadyM.Api.Generators.Duplication;

/// <summary>
/// Produces a struct that is a copy of another struct under a different name.
///
/// The target is named, not passed in as a symbol, so the usual case is that it does not exist yet and this call is
/// what brings it into existence: modifiers, type parameters, interfaces and members all come from the source.
///
/// If the compilation does declare a partial half under the target name, the engine finds it and defers to it.
/// Members that half declares are skipped, so redeclaring a member there replaces the copied one, and its own
/// modifiers win. That is how a duplicate gets customised without the caller having to describe the differences.
///
/// Duplication always returns source. Anything that went wrong is written into that source as a <c>#error</c>
/// directive rather than handed back for the caller to report: a file that names its own problem at the point of
/// use beats no file at all. Ordinary conflicts, such as the target name already being taken by something else,
/// are not commented on at all, because the compiler already says so.
///
/// The engine is deliberately standalone: it knows nothing about which attribute or generator asked for the copy,
/// only about a <see cref="TypeDuplicationRequest"/>. Move this folder as-is to share it.
/// </summary>
internal static class TypeDuplicator
{
    /// <summary>Duplicates the source struct, always returning a source file, problems included.</summary>
    public static string Duplicate(TypeDuplicationRequest request)
    {
        var source = request.Source;
        var errors = new List<string>();

        if (!SyntaxFacts.IsValidIdentifier(request.TargetName))
        {
            // Nothing can be written without a name to write it under.
            return Header(request, [$"'{request.TargetName}' is not a usable type name."]);
        }

        if (source.TypeKind != TypeKind.Struct)
            errors.Add($"'{source.ToDisplayString()}' is not a struct, so '{request.TargetName}' may not be either.");

        var sourceParts = source.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .ToList();

        if (sourceParts.Count == 0)
        {
            errors.Add(
                $"'{source.ToDisplayString()}' is not declared in this compilation, so its members cannot be copied. "
                + "Duplication works on source, not on referenced assemblies.");
        }

        var targetNamespace = request.ResolvedNamespace;

        // The target may or may not already have a hand-written partial half. Either is fine.
        var existing = FindExisting(request.Compilation, targetNamespace, request.TargetName);

        if (SymbolEqualityComparer.Default.Equals(existing, source))
            errors.Add($"'{source.ToDisplayString()}' cannot be a duplicate of itself.");
        else if (existing is not null && existing.Arity != source.Arity)
        {
            errors.Add(
                $"'{existing.ToDisplayString()}' has {existing.Arity} type parameter(s) but "
                + $"'{source.ToDisplayString()}' has {source.Arity}. They must match.");
        }

        var sourceQualified = source.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var targetQualified = "global::" + request.TargetFullName + TypeArgumentSuffix(source);

        var (declaredSignatures, declaredNames) = existing is null
            ? (new HashSet<string>(), new HashSet<string>())
            : MemberSignature.CollectDeclared(existing);

        var excluded = new HashSet<string>(request.ExcludedMemberNames, StringComparer.Ordinal);

        var copied = new List<MemberDeclarationSyntax>();
        var usings = new List<string>();
        var seenUsings = new HashSet<string>(StringComparer.Ordinal);
        var typeAttributes = new List<AttributeListSyntax>();

        SyntaxList<TypeParameterConstraintClauseSyntax> constraints = default;
        ParameterListSyntax? primaryConstructor = null;

        foreach (var part in sourceParts)
        {
            var semanticModel = request.Compilation.GetSemanticModel(part.SyntaxTree);
            var rewriter = new TypeReferenceRewriter(
                semanticModel,
                source,
                request.TargetName,
                request.CopyAttributes,
                request.CopyDocumentation);

            CollectUsings(part, usings, seenUsings);

            if (request.CopyTypeAttributes)
            {
                typeAttributes.AddRange(part.AttributeLists
                    .Select(list => rewriter.Visit(list))
                    .OfType<AttributeListSyntax>());
            }

            // Constraints may be written on any one part, and may mention the source type.
            if (constraints.Count == 0 && part.ConstraintClauses.Count > 0)
            {
                constraints = SyntaxFactory.List(part.ConstraintClauses
                    .Select(clause => rewriter.Visit(clause))
                    .OfType<TypeParameterConstraintClauseSyntax>());
            }

            // A primary constructor lives on the declaration, not among the members, so it is copied from here.
            // Its parameters are in scope in the copied member bodies, which is why it has to come across.
            if (primaryConstructor is null && part.ParameterList is not null)
                primaryConstructor = rewriter.Visit(part.ParameterList) as ParameterListSyntax;

            var kept = part.Members
                .Select(member => KeepMember(
                    member,
                    semanticModel,
                    sourceQualified,
                    targetQualified,
                    declaredSignatures,
                    declaredNames,
                    excluded))
                .Where(member => member is not null)
                .Select(member => member!);

            copied.AddRange(rewriter.RewriteAll(kept));
        }

        // Exactly one part may declare the parameter list, so a hand-written primary constructor wins.
        if (existing is not null && HasPrimaryConstructor(existing))
            primaryConstructor = null;

        // Unqualified references to the source type's siblings resolve through its own namespace.
        if (!source.ContainingNamespace.IsGlobalNamespace)
        {
            var sourceNamespaceUsing = "using " + source.ContainingNamespace.ToDisplayString() + ";";
            if (seenUsings.Add(sourceNamespaceUsing))
                usings.Add(sourceNamespaceUsing);
        }

        return Emit(
            request,
            errors,
            existing,
            targetNamespace,
            usings,
            typeAttributes,
            constraints,
            primaryConstructor,
            copied,
            sourceQualified,
            targetQualified);
    }

    /// <summary>
    /// Finds a hand-written declaration of the target name in this compilation, ignoring same-named types from
    /// referenced assemblies, which the generated type would simply shadow.
    /// </summary>
    private static INamedTypeSymbol? FindExisting(Compilation compilation, string? targetNamespace, string targetName)
    {
        var namespaceSymbol = ResolveNamespace(compilation.GlobalNamespace, targetNamespace);

        return namespaceSymbol?
            .GetTypeMembers(targetName)
            .FirstOrDefault(type =>
                type.DeclaringSyntaxReferences.Length > 0 &&
                SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly));
    }

    private static INamespaceSymbol? ResolveNamespace(INamespaceSymbol root, string? qualifiedName)
    {
        if (string.IsNullOrEmpty(qualifiedName))
            return root;

        var current = root;

        foreach (var part in qualifiedName!.Split('.'))
        {
            current = current.GetNamespaceMembers().FirstOrDefault(ns => ns.Name == part);

            if (current is null)
                return null;
        }

        return current;
    }

    /// <summary>Returns the member to copy, trimmed of excluded field/event variables, or <c>null</c> to skip it.</summary>
    private static MemberDeclarationSyntax? KeepMember(
        MemberDeclarationSyntax member,
        SemanticModel semanticModel,
        string sourceQualified,
        string targetQualified,
        HashSet<string> declaredSignatures,
        HashSet<string> declaredNames,
        HashSet<string> excluded)
    {
        // A field or event-field declaration can declare several names at once, so it is filtered variable by variable.
        switch (member)
        {
            case FieldDeclarationSyntax field:
            {
                var keptFields = FilterVariables(field.Declaration, semanticModel, declaredNames, excluded);
                return keptFields is null ? null : field.WithDeclaration(keptFields);
            }

            case EventFieldDeclarationSyntax eventField:
            {
                var keptEvents = FilterVariables(eventField.Declaration, semanticModel, declaredNames, excluded);
                return keptEvents is null ? null : eventField.WithDeclaration(keptEvents);
            }
        }

        var symbol = semanticModel.GetDeclaredSymbol(member);
        if (symbol is null)
            return null;

        if (excluded.Contains(symbol.Name))
            return null;

        var key = MemberSignature.Create(symbol, sourceQualified, targetQualified);
        if (key is null)
            return null;

        if (declaredSignatures.Contains(key))
            return null;

        // A non-overloadable member is blocked by anything the target declares under the same name.
        if (symbol is not IMethodSymbol &&
            symbol is not IPropertySymbol { IsIndexer: true } &&
            declaredNames.Contains(symbol.Name))
        {
            return null;
        }

        return member;
    }

    private static VariableDeclarationSyntax? FilterVariables(
        VariableDeclarationSyntax declaration,
        SemanticModel semanticModel,
        HashSet<string> declaredNames,
        HashSet<string> excluded)
    {
        var kept = declaration.Variables
            .Where(variable =>
            {
                var name = semanticModel.GetDeclaredSymbol(variable)?.Name ?? variable.Identifier.Text;
                return !excluded.Contains(name) && !declaredNames.Contains(name);
            })
            .ToList();

        if (kept.Count == 0)
            return null;

        if (kept.Count == declaration.Variables.Count)
            return declaration;

        return declaration.WithVariables(SyntaxFactory.SeparatedList(kept));
    }

    private static void CollectUsings(SyntaxNode part, List<string> usings, HashSet<string> seen)
    {
        foreach (var node in part.Ancestors())
        {
            SyntaxList<UsingDirectiveSyntax> directives;

            switch (node)
            {
                case CompilationUnitSyntax unit:
                    directives = unit.Usings;
                    break;

                case BaseNamespaceDeclarationSyntax ns:
                    directives = ns.Usings;
                    break;

                default:
                    continue;
            }

            foreach (var directive in directives)
            {
                var text = directive.ToString().Trim();
                if (seen.Add(text))
                    usings.Add(text);
            }
        }
    }

    /// <summary>
    /// The file header, and any problems as <c>#error</c> directives so they surface where the generated code is,
    /// not in some diagnostic the caller has to remember to forward.
    /// </summary>
    private static string Header(TypeDuplicationRequest request, IReadOnlyList<string> errors)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Duplicated from " + request.Source.ToDisplayString() + ".");

        foreach (var error in errors)
            sb.AppendLine("#error " + error.Replace("\r", " ").Replace("\n", " "));

        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        return sb.ToString();
    }

    private static string Emit(
        TypeDuplicationRequest request,
        IReadOnlyList<string> errors,
        INamedTypeSymbol? existing,
        string? targetNamespace,
        List<string> usings,
        List<AttributeListSyntax> typeAttributes,
        SyntaxList<TypeParameterConstraintClauseSyntax> constraints,
        ParameterListSyntax? primaryConstructor,
        List<MemberDeclarationSyntax> members,
        string sourceQualified,
        string targetQualified)
    {
        var sb = new StringBuilder(Header(request, errors));

        foreach (var directive in usings.OrderBy(u => u, StringComparer.Ordinal))
            sb.AppendLine(directive);

        if (usings.Count > 0)
            sb.AppendLine();

        if (targetNamespace is not null)
        {
            sb.AppendLine("namespace " + targetNamespace + ";");
            sb.AppendLine();
        }

        // Only an existing target can be nested, and then its enclosing types have to be reopened around it.
        var enclosing = new List<INamedTypeSymbol>();
        for (var containing = existing?.ContainingType; containing is not null; containing = containing.ContainingType)
            enclosing.Insert(0, containing);

        var indent = string.Empty;
        foreach (var containing in enclosing)
        {
            sb.AppendLine(indent + EnclosingHeader(containing));
            sb.AppendLine(indent + "{");
            indent += "    ";
        }

        foreach (var attributeList in typeAttributes)
            sb.AppendLine(Reindent(attributeList.ToString(), indent));

        sb.AppendLine(indent + TargetHeader(
            request,
            existing,
            primaryConstructor,
            BuildBaseList(request, existing, sourceQualified, targetQualified)));

        foreach (var clause in constraints)
            sb.AppendLine(Reindent(clause.ToString(), indent + "    "));

        sb.AppendLine(indent + "{");

        var memberIndent = indent + "    ";
        var first = true;

        foreach (var member in members)
        {
            if (!first)
                sb.AppendLine();

            first = false;
            sb.AppendLine(Reindent(member.ToFullString(), memberIndent));
        }

        if (!string.IsNullOrEmpty(request.InsertBlock))
            sb.AppendLine(request.InsertBlock);

        sb.AppendLine(indent + "}");

        for (var i = enclosing.Count - 1; i >= 0; i--)
        {
            indent = indent.Substring(0, i * 4);
            sb.AppendLine(indent + "}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// The declaration line for the produced type. Whatever an existing partial half already states wins, so the
    /// two halves cannot disagree; everything else is inherited from the source.
    /// </summary>
    private static string TargetHeader(
        TypeDuplicationRequest request,
        INamedTypeSymbol? existing,
        ParameterListSyntax? primaryConstructor,
        string? baseList)
    {
        var source = request.Source;

        var sb = new StringBuilder();
        sb.Append(AccessibilityKeyword(
            request.TargetAccessibility ?? existing?.DeclaredAccessibility ?? source.DeclaredAccessibility));

        if (existing?.IsReadOnly ?? source.IsReadOnly)
            sb.Append("readonly ");

        if (IsDeclaredUnsafe(existing) || IsDeclaredUnsafe(source))
            sb.Append("unsafe ");

        if (existing?.IsRefLikeType ?? source.IsRefLikeType)
            sb.Append("ref ");

        // An existing half is already partial, so the generated half has to be too.
        if (request.Partial || existing is not null)
            sb.Append("partial ");

        sb.Append((existing?.IsRecord ?? source.IsRecord) ? "record struct " : "struct ");
        sb.Append(request.TargetName);

        // With an existing half the type parameter names must match what it wrote; otherwise take the source's.
        var typeParameters = existing is not null ? existing.TypeParameters : source.TypeParameters;
        if (typeParameters.Length > 0)
            sb.Append('<').Append(string.Join(", ", typeParameters.Select(p => p.Name))).Append('>');

        if (primaryConstructor is not null)
            sb.Append(primaryConstructor.ToString());

        if (!string.IsNullOrEmpty(baseList))
            sb.Append(" : ").Append(baseList);

        return sb.ToString();
    }

    private static string EnclosingHeader(INamedTypeSymbol type)
    {
        var keyword = type.TypeKind switch
        {
            TypeKind.Struct => type.IsRecord ? "record struct" : "struct",
            TypeKind.Interface => "interface",
            _ => type.IsRecord ? "record" : "class"
        };

        var sb = new StringBuilder();
        sb.Append(AccessibilityKeyword(type.DeclaredAccessibility));

        if (type.IsStatic)
            sb.Append("static ");

        if (type.IsReadOnly)
            sb.Append("readonly ");

        if (IsDeclaredUnsafe(type))
            sb.Append("unsafe ");

        sb.Append("partial ").Append(keyword).Append(' ').Append(type.Name);

        if (type.Arity > 0)
            sb.Append('<').Append(string.Join(", ", type.TypeParameters.Select(p => p.Name))).Append('>');

        return sb.ToString();
    }

    private static string? BuildBaseList(
        TypeDuplicationRequest request,
        INamedTypeSymbol? existing,
        string sourceQualified,
        string targetQualified)
    {
        if (!request.CopyInterfaces)
            return null;

        var already = new HashSet<string>(
            (existing?.AllInterfaces ?? []).Select(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
            StringComparer.Ordinal);

        var added = request.Source.Interfaces
            .Select(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace(sourceQualified, targetQualified))
            .Where(name => !already.Contains(name))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return added.Count == 0 ? null : string.Join(", ", added);
    }

    /// <summary>The <c>&lt;T, U&gt;</c> suffix that makes a bare name a usable type reference.</summary>
    private static string TypeArgumentSuffix(INamedTypeSymbol type)
        => type.Arity == 0
            ? string.Empty
            : "<" + string.Join(", ", type.TypeParameters.Select(p => p.Name)) + ">";

    private static bool HasPrimaryConstructor(INamedTypeSymbol type)
        => type.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(declaration => declaration.ParameterList is not null);

    private static bool IsDeclaredUnsafe(INamedTypeSymbol? type)
        => type is not null &&
           type.DeclaringSyntaxReferences
               .Select(reference => reference.GetSyntax())
               .OfType<TypeDeclarationSyntax>()
               .Any(declaration => declaration.Modifiers.Any(SyntaxKind.UnsafeKeyword));

    private static string AccessibilityKeyword(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => "public ",
        Accessibility.Internal => "internal ",
        Accessibility.Private => "private ",
        Accessibility.Protected => "protected ",
        Accessibility.ProtectedOrInternal => "protected internal ",
        Accessibility.ProtectedAndInternal => "private protected ",
        _ => string.Empty
    };

    private static INamedTypeSymbol? FindNonPartialDeclaration(INamedTypeSymbol target)
    {
        for (var type = target; type is not null; type = type.ContainingType)
        {
            var declarations = type.DeclaringSyntaxReferences
                .Select(reference => reference.GetSyntax())
                .OfType<TypeDeclarationSyntax>()
                .ToList();

            if (declarations.Count > 0 && !declarations.Any(d => d.Modifiers.Any(SyntaxKind.PartialKeyword)))
                return type;
        }

        return null;
    }

    /// <summary>Strips the original indentation and re-indents for the position in the generated file.</summary>
    private static string Reindent(string text, string indent)
    {
        var lines = text.Replace("\r\n", "\n").Trim('\n').TrimEnd().Split('\n');

        var common = int.MaxValue;
        foreach (var line in lines)
        {
            if (line.Trim().Length == 0)
                continue;

            var leading = line.Length - line.TrimStart().Length;
            common = Math.Min(common, leading);
        }

        if (common == int.MaxValue)
            common = 0;

        var sb = new StringBuilder();
        var first = true;

        foreach (var line in lines)
        {
            if (!first)
                sb.AppendLine();

            first = false;

            if (line.Trim().Length == 0)
                continue;

            sb.Append(indent).Append(line.Substring(Math.Min(common, line.Length)));
        }

        return sb.ToString();
    }
}
