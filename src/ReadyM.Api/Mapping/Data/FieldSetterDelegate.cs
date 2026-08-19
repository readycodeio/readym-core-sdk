using Friflo.Engine.ECS;

namespace ReadyM.Api.Mapping.Data;

/// <exclude />
public delegate void FieldSetterDelegate<TComponent, in TValue>(ref TComponent component, TValue value)
    where TComponent : struct;

/// <exclude />
public delegate void FieldSetterFromApiDelegate<TComponent, in TValue>(ref TComponent component, TValue value, Entity entity)
    where TComponent : struct;
