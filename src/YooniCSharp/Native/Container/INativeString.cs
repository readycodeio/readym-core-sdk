namespace Yooni.Native.Container;

public interface INativeString
{
    int Length { get; }
    int Capacity { get; }
    
    string ToManaged();
    public unsafe void CopyTo(byte* dest);

    bool Equals(string? str);
}