namespace ReadyM.SDK.Attributes;

/// <summary>
/// Indicates the entry point of a mod.
/// The annotated class may declare a <c>private void Start()</c> method, which will be called when the mod is loaded.
/// </summary>
/// <remarks>
/// You can inject dependencies into the constructor of the annotated class, which will be resolved from the dependency container.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ModEntryAttribute : Attribute;
