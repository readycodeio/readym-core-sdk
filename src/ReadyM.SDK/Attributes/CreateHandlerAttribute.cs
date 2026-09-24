namespace ReadyM.SDK.Attributes;

/// Runs this method on an entity of the declaring shape right after it is created.
[AttributeUsage(AttributeTargets.Method)]
public sealed class CreateHandlerAttribute : Attribute;
