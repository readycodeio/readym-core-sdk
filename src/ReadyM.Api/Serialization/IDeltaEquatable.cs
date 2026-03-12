namespace ReadyM.Api.Serialization;

public interface IDeltaEquatable<in T>
{
    bool DeltaEquals(T other, float delta);
}
