namespace ReadyM.Api.Serialization;

public interface IDeltaEquatable<in T>
{
    public bool DeltaEquals(T other, float delta);
}