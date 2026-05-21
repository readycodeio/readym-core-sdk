using ReadyM.Api.Generators.TypeTranslation.Model;

namespace ReadyM.Api.Generators.TypeTranslation.Rendering;

internal interface ITypeRenderer
{
    string Render(ITypeName typeName);
}