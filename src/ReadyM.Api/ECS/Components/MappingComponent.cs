using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Components;

internal readonly struct MappingComponent<TGameObject>(TGameObject? gameObject) 
    : IIndexedComponent<TGameObject?>
    where TGameObject : class
{
    private readonly TGameObject? _gameObject = gameObject;
    
    public TGameObject? GameObject
        => _gameObject;

    public TGameObject? GetIndexedValue()
        => _gameObject;

    private bool Equals(MappingComponent<TGameObject> other)
        => Equals(_gameObject, other._gameObject);

    public override bool Equals(object? obj)
        => obj is MappingComponent<TGameObject> other && Equals(other);

    public override int GetHashCode()
        => _gameObject != null ? _gameObject.GetHashCode() : 0;

    public static bool operator ==(MappingComponent<TGameObject> left, MappingComponent<TGameObject> right)
        => Equals(left._gameObject, right._gameObject);

    public static bool operator !=(MappingComponent<TGameObject> left, MappingComponent<TGameObject> right)
        => !Equals(left._gameObject, right._gameObject);
}