namespace ReadyM.Api.Mapping.Api;

public readonly struct Field<TComponent, TValue>
    where TComponent : struct
{
    public readonly int Id;
    public Field(int id) => Id = id;
    public Field<TComponent, TValue, TContext> In<TContext>() => new(Id);

    public static implicit operator int(Field<TComponent, TValue> field) => field.Id;
}

public readonly struct Field<TComponent, TValue, TContext>
    where TComponent : struct
{
    public readonly int Id;
    public Field(int id) => Id = id;
}

public readonly struct BoundField<TComponent, TValue>
    where TComponent : struct
{
    public readonly int Id;
    public BoundField(int id) => Id = id;
}

public readonly struct BoundField<TComponent, TValue, TContext>
    where TComponent : struct
{
    public readonly int Id;
    public BoundField(int id) => Id = id;
}