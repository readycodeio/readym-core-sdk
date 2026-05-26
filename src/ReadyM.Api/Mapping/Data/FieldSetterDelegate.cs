namespace ReadyM.Api.Mapping.Data;

/// <exclude />
public delegate void FieldSetterDelegate<TComponent, in TValue>(ref TComponent component, TValue value)
    where TComponent : struct;