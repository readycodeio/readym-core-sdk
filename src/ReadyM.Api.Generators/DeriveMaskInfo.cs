using System;

namespace ReadyM.Api.Generators;

internal sealed class DeriveMaskInfo(string csharpType, string cppType, string readMethod, int bits, bool invalid)
{
    public string CSharpType { get; } = csharpType ?? throw new ArgumentNullException(nameof(csharpType));
    public string CppType { get; } = cppType ?? throw new ArgumentNullException(nameof(cppType));
    public string ReadMethod { get; } = readMethod ?? throw new ArgumentNullException(nameof(readMethod));
    public int Bits { get; } = bits;
    public bool Invalid { get; } = invalid;
}