using ReadyM.Api.Generators.Derive.Cpp;
using ReadyM.Api.Generators.TypeTranslation.Model;
using Xunit;

namespace ReadyM.Api.Generators.Tests.TypeTranslation;

public class CppTypeTranslationPipelineTests
{
    [Fact]
    public void PathTranslation_Works()
    {
        var pipeline = CppTypeTranslationPipeline.PathTranslation;
       
        var input = TypeNameFactory.Qualified("Yooni", "Native", "Container", "NativeString256");
        
        var output = pipeline.Translate(input);
        var rendered = pipeline.Render(output);

        Assert.Equal("Native/Container/NativeString.h", rendered);
    }
    
    [Fact]
    public void TypeTranslation_Works()
    {
        var pipeline = CppTypeTranslationPipeline.TypeTranslation;

        var input = TypeNameFactory.Generic(
            TypeNameFactory.Qualified("Yooni", "Native", "Container", "NativeString256"),
            TypeNameFactory.Qualified("System", "String"),
            TypeNameFactory.Name("int"),
            TypeNameFactory.Name("string")
        );
        
        var output = pipeline.Translate(input);
        var rendered = pipeline.Render(output);

        Assert.Equal("Yooni::Native::Container::NativeString256<Interop::String, int32_t, Interop::String>", rendered);
    }
}