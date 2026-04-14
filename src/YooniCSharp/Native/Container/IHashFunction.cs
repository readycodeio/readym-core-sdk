namespace Yooni.Native.Container;

public interface IHashFunction<in T>
{
    uint ComputeHash(T value);
}