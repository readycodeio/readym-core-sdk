using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReadyM.Api.Generators.Derive.CSharp;
using ReadyM.Api.Generators.Derive.CSharp.ConflictResolution;
using ReadyM.Api.Generators.Derive.CSharp.FieldSupport;
using static ReadyM.Api.Generators.DeriveCSharpUtils;

namespace ReadyM.Api.Generators;

[Generator]
[SuppressMessage("MicrosoftCodeAnalysisDesign", "RS1032:Define diagnostic message correctly")]
internal sealed class DeriveINetworkedComponentGenerator : IIncrementalGenerator
{
    public struct TransformResult(
        string genName,
        string genCode,
        string? errorMessage,
        Exception? exception)
    {
        public string GenName { get; private set; } = genName;
        public string GenCode { get; private set; } = genCode;
        public string? ErrorMessage { get; private set; } = errorMessage;
        public Exception? Exception { get; private set; } = exception;

        public static TransformResult Success(string genName, string genCode)
            => new(genName, genCode, null, null);
        public static TransformResult Failure(string errorMessage)
            => new(string.Empty, string.Empty, errorMessage, null);
        public static TransformResult FatalError(Exception? exception = null)
            => new(string.Empty, string.Empty, null, exception);
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var codeProvider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: Predicate,
            transform: Transform);

        context.RegisterSourceOutput(
            codeProvider,
            (spc, result) =>
            {
                if (result.Exception is not null)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "DNC001",
                            title: $"{nameof(DeriveINetworkedComponentGenerator)} generator failed",
                            messageFormat: "Source generator failed: {0}",
                            category: "Generator",
                            defaultSeverity: DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        Location.None,
                        string.Join("\\n", result.Exception.ToString().Split(["\r\n", "\r", "\n"], StringSplitOptions.None))));

                    return;
                }
                else if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "DNC002",
                            title: $"{nameof(DeriveINetworkedComponentGenerator)} generator failed",
                            messageFormat: "Source generator failed: {0}",
                            category: "Generator",
                            defaultSeverity: DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        Location.None,
                        result.ErrorMessage));

                    return;
                }

                spc.AddSource($"{result.GenName}.g.cs", result.GenCode);
            });
    }

    private bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return false;

        if (syntaxNode is not StructDeclarationSyntax { AttributeLists.Count: > 0 } structDecl)
            return false;

        var attributes = structDecl.AttributeLists.SelectMany(static x => x.Attributes).ToList();
        return attributes.Any(x => x.Name is IdentifierNameSyntax { Identifier.Text: "DeriveINetworkedComponent" or "DeriveINetworkedComponentAttribute" });
    }

    private TransformResult Transform(
        GeneratorSyntaxContext context,
        CancellationToken ct)
    {
        try
        {
            if (ct.IsCancellationRequested)
                return default;

            var symbol = DeriveUtils.GetTargetSymbol(context, ct);
            var targetModel = DeriveComponentUtils.GetTargetModel(false, symbol, context);
            var genCode = ReplicatedComponentEmitter.Emit(targetModel);
            var genName = $"{DeriveUtils.GetGeneratedFileName(symbol)}";

            return TransformResult.Success(genName, genCode);
        }
        catch (Exception ex)
        {
            return TransformResult.FatalError(ex);
        }
    }
}
