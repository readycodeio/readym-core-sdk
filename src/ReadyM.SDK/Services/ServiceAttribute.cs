namespace ReadyM.SDK.Services;

/// <summary>
/// Declares a class as a service.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ServiceAttribute : Attribute;

/// <summary>
/// Declares a method as an update handler.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class UpdateHandlerAttribute : Attribute;