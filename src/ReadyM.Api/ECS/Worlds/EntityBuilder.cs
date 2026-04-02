using Friflo.Engine.ECS;
using ReadyM.Api.Generators;

namespace ReadyM.Api.ECS.Worlds;

[WrapperFor(typeof(CreateEntityBatch))]
[WrapperInclude("Add")]
internal sealed partial class EntityBuilder
{
    internal EntityBuilder(CreateEntityBatch wrapped)
    {
        _wrapped = wrapped;
    }
}