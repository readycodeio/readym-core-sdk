using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace ReadyM.Api.Generators.Tests;

internal static class SourceGeneratorTestHelper
{
    internal sealed class GeneratorRunResult(
        ImmutableArray<Diagnostic> compilationDiagnostics,
        Compilation inputCompilation,
        Compilation outputCompilation,
        GeneratorDriverRunResult driverRunResult)
    {
        public ImmutableArray<Diagnostic> CompilationDiagnostics { get; } = compilationDiagnostics;
        public Compilation InputCompilation { get; } = inputCompilation ?? throw new ArgumentNullException(nameof(inputCompilation));

        public Compilation OutputCompilation { get; } = outputCompilation ?? throw new ArgumentNullException(nameof(outputCompilation));

        public GeneratorDriverRunResult DriverRunResult { get; } = driverRunResult;

        public ImmutableArray<Diagnostic> InputDiagnostics => InputCompilation.GetDiagnostics();

        public ImmutableArray<Diagnostic> OutputDiagnostics => OutputCompilation.GetDiagnostics();

        public IEnumerable<SyntaxTree> GeneratedSyntaxTrees =>
            OutputCompilation.SyntaxTrees.Except(InputCompilation.SyntaxTrees);
    }

    public static GeneratorRunResult RunGenerator<TGenerator>(string source, ITestOutputHelper output)
        where TGenerator : IIncrementalGenerator, new()
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        return RunGenerator<TGenerator>(
            [("TestInput.cs", source)],
            output);
    }

    public static GeneratorRunResult RunGenerator<TGenerator>(
        IEnumerable<(string Path, string Source)> sources,
        ITestOutputHelper output)
        where TGenerator : IIncrementalGenerator, new()
    {
        if (sources is null)
            throw new ArgumentNullException(nameof(sources));

        var inputCompilation = CreateCompilation(sources, output);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new TGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(
            inputCompilation,
            out var outputCompilation,
            out var compilationDiagnostics);

        var runResult = driver.GetRunResult();
        var result = new GeneratorRunResult(compilationDiagnostics, inputCompilation, outputCompilation, runResult);

        WriteGeneratedFiles(output, result);

        return result;
    }

    public static Assembly EmitToAssembly(Compilation compilation, ITestOutputHelper output)
    {
        if (compilation is null)
            throw new ArgumentNullException(nameof(compilation));

        using var peStream = new MemoryStream();
        using var pdbStream = new MemoryStream();

        var emitResult = compilation.Emit(
            peStream,
            pdbStream,
            options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));

        if (!emitResult.Success)
        {
            WriteAllSyntaxTrees(output, compilation);
            throw new InvalidOperationException(BuildCompilationFailureMessage(compilation, emitResult.Diagnostics));
        }

        peStream.Position = 0;
        pdbStream.Position = 0;

        return AssemblyLoadContext.Default.LoadFromStream(peStream, pdbStream);
    }

    public static Compilation CreateCompilation(string source, ITestOutputHelper output)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        return CreateCompilation(
            [("TestInput.cs", source)],
            output);
    }

    public static Compilation CreateCompilation(
        IEnumerable<(string Path, string Source)> sources,
        ITestOutputHelper output)
    {
        if (sources is null)
            throw new ArgumentNullException(nameof(sources));

        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);

        var syntaxTrees = sources
            .Select(source =>
            {
                if (source.Source is null)
                    throw new ArgumentNullException(nameof(source.Source));

                var sourceText = SourceText.From(source.Source, Encoding.UTF8);
                return CSharpSyntaxTree.ParseText(
                    sourceText,
                    parseOptions,
                    path: string.IsNullOrWhiteSpace(source.Path)
                        ? "TestInput.cs"
                        : source.Path);
            })
            .ToArray();

        return CSharpCompilation.Create(
            assemblyName: "ReadyM.Api.Generators.Tests.Dynamic_" + Guid.NewGuid().ToString("N"),
            syntaxTrees: syntaxTrees,
            references: GetMetadataReferences(output),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable,
                optimizationLevel: OptimizationLevel.Debug,
                allowUnsafe: true));
    }

    private static void WriteGeneratedFiles(ITestOutputHelper output, GeneratorRunResult result)
    {
        foreach (var diagnostic in result.CompilationDiagnostics)
        {
            output.WriteLine($"DIAGNOSTIC: {diagnostic}");
        }

        output.WriteLine("===== GENERATED FILES =====");
        output.WriteLine(string.Empty);

        var generatedTrees = result.GeneratedSyntaxTrees.ToArray();
        if (generatedTrees.Length == 0)
        {
            output.WriteLine("<none>");
            output.WriteLine(string.Empty);
            return;
        }

        foreach (var syntaxTree in generatedTrees)
        {
            output.WriteLine("----- FILE: " + GetDisplayPath(syntaxTree) + " -----");
            output.WriteLine(string.Empty);

            var text = syntaxTree.GetText().ToString();
            var lineNumber = 1;
            foreach (var line in text.EnumerateLines())
            {
                output.WriteLine($"{lineNumber++:D4}: {line}");
            }

            if (!text.EndsWith(Environment.NewLine, StringComparison.Ordinal))
            {
                output.WriteLine(string.Empty);
            }

            output.WriteLine("----- END FILE -----");
            output.WriteLine(string.Empty);
        }
    }

    private static void WriteAllSyntaxTrees(ITestOutputHelper output, Compilation compilation)
    {
        output.WriteLine("===== ALL SYNTAX TREES =====");
        output.WriteLine(string.Empty);

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            output.WriteLine("----- FILE: " + GetDisplayPath(syntaxTree) + " -----");
            output.WriteLine(string.Empty);

            var text = syntaxTree.GetText().ToString();
            output.WriteLine(text);

            if (!text.EndsWith(Environment.NewLine, StringComparison.Ordinal))
            {
                output.WriteLine(string.Empty);
            }

            output.WriteLine("----- END FILE -----");
            output.WriteLine(string.Empty);
        }
    }

    private static string BuildCompilationFailureMessage(Compilation compilation, IEnumerable<Diagnostic> diagnostics)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Compilation emit failed:");
        sb.AppendLine();

        foreach (var diagnostic in diagnostics.OrderBy(d => d.Location.SourceTree?.FilePath).ThenBy(d => d.Location.SourceSpan.Start))
        {
            sb.AppendLine(diagnostic.ToString());
        }

        sb.AppendLine();
        sb.AppendLine("===== Syntax Trees =====");
        sb.AppendLine();

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            sb.AppendLine("----- FILE: " + GetDisplayPath(syntaxTree) + " -----");
            sb.AppendLine();

            var text = syntaxTree.GetText().ToString();
            sb.AppendLine(text);

            if (!text.EndsWith(Environment.NewLine, StringComparison.Ordinal))
            {
                sb.AppendLine();
            }

            sb.AppendLine("----- END FILE -----");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string GetDisplayPath(SyntaxTree syntaxTree)
    {
        if (!string.IsNullOrWhiteSpace(syntaxTree.FilePath))
            return syntaxTree.FilePath;

        return "<unknown>";
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences(ITestOutputHelper output)
    {
        var assemblies = new[]
        {
            typeof(object).Assembly,
            typeof(Attribute).Assembly,
            typeof(Enumerable).Assembly,
            typeof(Console).Assembly,
            typeof(System.Numerics.Vector2).Assembly,
            typeof(System.Runtime.CompilerServices.Unsafe).Assembly,
            typeof(CSharpCompilation).Assembly,

            typeof(ReadyM.Api.Multiplayer.Generators.DeriveINetworkedComponentAttribute).Assembly,
            typeof(ReadyM.Api.Multiplayer.ECS.Components.INetworkedComponent).Assembly,
            typeof(LiteNetLib.Utils.NetDataWriter).Assembly,
            typeof(Yooni.Native.Container.ByteHash).Assembly,
            typeof(Yooni.Native.LowLevel.Allocator).Assembly,
            typeof(Yooni.Native.Serialization.NetSerializationExtensions).Assembly,

            typeof(TestAssemblyMarker).Assembly
        };

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in assemblies)
        {
            AddAssemblyAndReferencesRecursive(assembly, seen, output);
        }

        foreach (var path in seen)
        {
            yield return MetadataReference.CreateFromFile(path);
        }
    }

    private static void AddAssemblyAndReferencesRecursive(Assembly assembly, HashSet<string> collectedPaths, ITestOutputHelper? output)
    {
        if (assembly.IsDynamic)
            return;

        if (string.IsNullOrWhiteSpace(assembly.Location))
            return;

        if (!collectedPaths.Add(assembly.Location))
            return;

        foreach (var referencedAssemblyName in assembly.GetReferencedAssemblies())
        {
            try
            {
                var referencedAssembly = Assembly.Load(referencedAssemblyName);
                AddAssemblyAndReferencesRecursive(referencedAssembly, collectedPaths, output);
            }
            catch (FileNotFoundException ex)
            {
                output?.WriteLine("Skipping unresolved referenced assembly '" + referencedAssemblyName.FullName + "': " + ex.Message);
            }
            catch (FileLoadException ex)
            {
                output?.WriteLine("Skipping unloadable referenced assembly '" + referencedAssemblyName.FullName + "': " + ex.Message);
            }
            catch (BadImageFormatException ex)
            {
                output?.WriteLine("Skipping invalid referenced assembly '" + referencedAssemblyName.FullName + "': " + ex.Message);
            }
        }
    }

    private sealed class TestAssemblyMarker
    {
    }
}
