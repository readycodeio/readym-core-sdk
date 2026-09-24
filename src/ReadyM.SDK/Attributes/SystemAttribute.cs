namespace ReadyM.SDK.Attributes;

/// Declares a system: a class with update handlers that are called on every update tick.
/// In game, this means every frame.
/// On the server, this means every tick of the server's update loop.
[AttributeUsage(AttributeTargets.Class)]
public sealed class SystemAttribute : Attribute;
