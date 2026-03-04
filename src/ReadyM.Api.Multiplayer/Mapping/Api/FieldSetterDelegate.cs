using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Mapping.Api;

public delegate void FieldSetterDelegate<TComponent, in TValue>(ref TComponent component, TValue value)
    where TComponent : IComponent;