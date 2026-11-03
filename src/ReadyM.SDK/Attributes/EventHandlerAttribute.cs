namespace ReadyM.SDK.Attributes;

/// <summary>
/// Declares a method as a network event handler.
/// The project it is compiled into says which side runs it - game or server.
/// </summary>
/// <param name="stage">Which stage of event propagation this method handles.</param>
/// <example>
/// Validate, Apply, Observe, and Rejected need an <c>in</c> parameter for the event, and optionally an <c>EventContext</c> parameter.
/// <code>
/// [EventHandler(Stage.Validate)] // Apply, Observe
/// public void CheckPlayerCanJoin(in PlayerJoinedEvent e, EventContext ctx)
/// </code>
/// Filter needs a <c>ref</c>, as it can change the event before it's applied.
/// <code>
/// [EventHandler(Stage.Filter)]
/// public void ChangePlayerJoinParams(ref PlayerJoinedEvent e, EventContext ctx)
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class EventHandlerAttribute(Stage stage) : Attribute
{
    /// <summary>Which stage of event propagation this method handles.</summary>
    public Stage Stage { get; } = stage;

    /// <summary>
    /// On <see cref="Stage.Apply"/> only: skip this handler on the client that raised the event, because that
    /// client already did what it applies.
    /// </summary>
    public bool SkipOnRaisingClient { get; set; }

    // TODO: Before?
    /// <summary>
    /// The manifest id of a mod this handler runs after, for the rare case where dependency order is not enough.
    /// </summary>
    public string? After { get; set; }
}