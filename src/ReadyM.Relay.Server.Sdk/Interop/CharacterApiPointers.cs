using System.Runtime.InteropServices;

namespace ReadyM.Relay.Server.Sdk.Interop;

/// <exclude />
[StructLayout(LayoutKind.Sequential)]
public struct CharacterApiPointers
{
    public required IntPtr GetCharacters;
    public required IntPtr GetCurrentCharacter;
    public required IntPtr GetLastSelectedCharacter;
    public required IntPtr CreateCharacter;
    public required IntPtr SelectCharacter;
}
