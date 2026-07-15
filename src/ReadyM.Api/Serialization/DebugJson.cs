using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace ReadyM.Api.Serialization;

internal static class DebugJson
{
    public static void Assert([DoesNotReturnIf(false)] bool condition, string? message = null)
    {
        if (!condition)
            throw new JsonException(message);
    }
}