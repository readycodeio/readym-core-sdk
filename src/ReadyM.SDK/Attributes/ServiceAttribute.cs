namespace ReadyM.SDK.Attributes;

/// <summary>
/// Declares a service class.
/// </summary>
/// <code>
/// [Service]
/// public sealed partial class MyService(MyDependency dep)
/// {
///     private void Start() { ... }
///     private void Update() { ... }
///     private void OnEnabled() { ... }
///     private void OnDisabled() { ... }
/// }
/// </code>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ServiceAttribute : Attribute;
