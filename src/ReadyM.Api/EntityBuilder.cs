using Friflo.Engine.ECS;
using ReadyM.Relay.Common.ECS.Generators;

namespace ReadyM.Api;

[WrapperFor(typeof(CreateEntityBatch))]
[WrapperInclude("Add")]
public sealed partial class EntityBuilder
{
    internal EntityBuilder(CreateEntityBatch wrapped)
    {
        _wrapped = wrapped;
    }
}