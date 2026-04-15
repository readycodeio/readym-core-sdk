using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.TypeTranslation;
using ReadyM.Api.Generators.TypeTranslation.Model;
using ReadyM.Api.Generators.TypeTranslation.Parsing;
using ReadyM.Api.Generators.TypeTranslation.Rendering;
using ReadyM.Api.Generators.TypeTranslation.Rules;

namespace ReadyM.Api.Generators.Derive.Cpp;

public static class CppTypeTranslationPipeline
{
    public static TypeTranslationPipeline CreatePipeline()
    {
        var parser = new RoslynTypeNameParser();
        var translator = new TypeNameTranslator(new List<ITypeNameRule>()
        {
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
    
    public static readonly TypeTranslationPipeline Instance = CreatePipeline();
}