namespace ReadyM.Api.Serialization;

/// <exclude />
public interface IDeltaEquatable<in T>
{
    bool DeltaEquals(T other, float delta);
}
