namespace ReadyM.Native.Container;

public interface IHashFunction<T>
{
    uint ComputeHash(in T value);
}