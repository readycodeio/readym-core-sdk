namespace ReadyM.Api.Multiplayer.Mapping.Data;

/// <exclude />
public delegate void FieldSetterDelegate<TComponent, in TValue>(ref TComponent component, TValue value)
    where TComponent : struct;