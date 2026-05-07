using ReadyM.Api.Generators.TypeTranslation.Model;

namespace ReadyM.Api.Generators.TypeTranslation.Rendering;

public interface ITypeRenderer
{
    string Render(ITypeName typeName);
}