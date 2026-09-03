using System;

namespace ReadyM.Api.Loader;

/// <summary>Thrown when a mod's config file exists but cannot be used.</summary>
public sealed class ModConfigException : Exception
{
    /// <inheritdoc />
    public ModConfigException(string message) : base(message) { }

    /// <inheritdoc />
    public ModConfigException(string message, Exception innerException) : base(message, innerException) { }
}