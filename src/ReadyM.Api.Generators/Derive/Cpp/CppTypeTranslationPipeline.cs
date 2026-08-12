using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.TypeTranslation;
using ReadyM.Api.Generators.TypeTranslation.Model;
using ReadyM.Api.Generators.TypeTranslation.Parsing;
using ReadyM.Api.Generators.TypeTranslation.Rendering;
using ReadyM.Api.Generators.TypeTranslation.Rules;

namespace ReadyM.Api.Generators.Derive.Cpp;

internal static class CppTypeTranslationPipeline
{
    public static TypeTranslationPipeline CreateTypeTranslationPipeline()
    {
        var parser = new RoslynTypeNameParser();
        var translator = new TypeNameTranslator(new List<ITypeNameRule>()
        {
            new NamespaceReplacementRule(TypeNameFactory.Qualified("OblivionMpCSharpMod"),
                TypeNameFactory.Name("RM")),
            new NamespaceReplacementRule(TypeNameFactory.Qualified("ReadyM", "Relay", "Common", "Oblivion"),
                TypeNameFactory.Name("RM")),
            new NamespaceReplacementRule(TypeNameFactory.Qualified("ReadyM", "Relay", "Common"),
                TypeNameFactory.Name("RM")),
            new NamespaceReplacementRule(TypeNameFactory.Qualified("ReadyM", "Relay"),
                TypeNameFactory.Name("RM")),
            new NamespaceReplacementRule(TypeNameFactory.Qualified("ReadyM", "Api", "Multiplayer"),
                TypeNameFactory.Name("RM")),
            new NamespaceReplacementRule(TypeNameFactory.Qualified("ReadyM", "Api"),
                TypeNameFactory.Name("RM")),
            new NamespaceReplacementRule(TypeNameFactory.Name("ReadyM"),
                TypeNameFactory.Name("RM")),
            new NamespaceReplacementRule(TypeNameFactory.Qualified("System", "Numerics"),
                TypeNameFactory.Name("Interop")),
            new NamespaceReplacementRule(TypeNameFactory.Qualified("System", "IntPtr"),
                TypeNameFactory.Qualified("System", "IntPtr")),
            new NamespaceReplacementRule(TypeNameFactory.Qualified("System", "UIntPtr"),
                TypeNameFactory.Qualified("System", "UIntPtr")),
            new NamespaceReplacementRule(TypeNameFactory.Name("System"), 
                TypeNameFactory.Name("Interop")),
            new GenericPatternTypeNameRule(
                TypeNameFactory.Generic(
                    TypeNameFactory.Qualified("Yooni", "Native", "LowLevel", "Storage8"),
                    TypeNameFactory.Param("T")),
                TypeNameFactory.Number(8)),
            new GenericPatternTypeNameRule(
                TypeNameFactory.Generic(
                    TypeNameFactory.Qualified("Yooni", "Native", "LowLevel", "Storage16"),
                    TypeNameFactory.Param("T")),
                TypeNameFactory.Number(16)),
            new GenericPatternTypeNameRule(
                TypeNameFactory.Generic(
                    TypeNameFactory.Qualified("Yooni", "Native", "LowLevel", "Storage32"),
                    TypeNameFactory.Param("T")),
                TypeNameFactory.Number(32)),
        });
        var renderer = new CppTypeRenderer();
        
        return new TypeTranslationPipeline(
            parser,
            translator,
            renderer);
    }
    
    public static TypeTranslationPipeline CreatePathTranslationPipeline()
    {
        var parser = new RoslynTypeNameParser();
        var translator = new TypeNameTranslator(new List<ITypeNameRule>()
        {
            new NamespaceReplacementRule(TypeNameFactory.Qualified("OblivionMpCSharpMod"),
                TypeNameFactory.Empty()),
            new NamespaceReplacementRule(TypeNameFactory.Qualified("ReadyM", "Relay", "Common", "Oblivion"),
                TypeNameFactory.Empty()),
            new NamespaceReplacementRule(TypeNameFactory.Qualified("ReadyM", "Relay", "Common"),
                TypeNameFactory.Empty()),
            new NamespaceReplacementRule(TypeNameFactory.Qualified("ReadyM", "Relay"),
                TypeNameFactory.Empty()),
            new NamespaceReplacementRule(TypeNameFactory.Qualified("ReadyM", "Api", "Multiplayer"),
                TypeNameFactory.Empty()),
            new NamespaceReplacementRule(TypeNameFactory.Qualified("ReadyM", "Api"),
                TypeNameFactory.Empty()),
            new NamespaceReplacementRule(TypeNameFactory.Name("ReadyM"),
                TypeNameFactory.Empty()),
            new NamespaceReplacementRule(TypeNameFactory.Qualified("System", "Numerics"),
                TypeNameFactory.Name("Interop")),
            new NamespaceReplacementRule(TypeNameFactory.Qualified("System", "IntPtr"),
                TypeNameFactory.Empty()),
            new NamespaceReplacementRule(TypeNameFactory.Qualified("System", "UIntPtr"),
                TypeNameFactory.Empty()),
            new NamespaceReplacementRule(TypeNameFactory.Name("System"),
                TypeNameFactory.Name("Interop")),
            new ExactTypeReplacementRule(
                TypeNameFactory.Qualified("Yooni", "Native", "Container", "ByteHash"),
                TypeNameFactory.Qualified("Native", "Container", "IntHash")),
            new ExactTypeReplacementRule(
                TypeNameFactory.Qualified("Yooni", "Native", "Container", "NativeString256"),
                TypeNameFactory.Qualified("Native", "Container", "NativeString")),
            new ExactTypeReplacementRule(
                TypeNameFactory.Qualified("Yooni", "Native", "Container", "NativeString64"),
                TypeNameFactory.Qualified("Native", "Container", "NativeString")),
            new ExactTypeReplacementRule(
                TypeNameFactory.Qualified("Yooni", "Native", "Container", "NativeStringHash256"),
                TypeNameFactory.Qualified("Native", "Container", "NativeStringHash")),
            new ExactTypeReplacementRule(
                TypeNameFactory.Qualified("Yooni", "Native", "Container", "NativeStringHash64"),
                TypeNameFactory.Qualified("Native", "Container", "NativeStringHash")),
            new NamespaceReplacementRule(TypeNameFactory.Name("Yooni"),
                TypeNameFactory.Empty()),
        });
        var renderer = new CppPathRenderer();
        
        return new TypeTranslationPipeline(
            parser,
            translator,
            renderer);
    }
    
    public static readonly TypeTranslationPipeline TypeTranslation = CreateTypeTranslationPipeline();
    public static readonly TypeTranslationPipeline PathTranslation = CreatePathTranslationPipeline();
}