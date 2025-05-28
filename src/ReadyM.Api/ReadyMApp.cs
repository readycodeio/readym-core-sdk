using Friflo.Engine.ECS;

namespace ReadyM.Api;

public static class ReadyMApp
{
    public static Store CreateEntityStore() => new(new EntityStore());
}