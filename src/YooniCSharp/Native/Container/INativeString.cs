namespace Yooni.Native.Container;

public interface INativeString
{
    int Length { get; }
    int Capacity { get; }
    
    string ToManaged();
    unsafe byte* GetChars();

    bool Equals(string? str);
}