namespace ReadyM.Api.Mapping.Events;

/// <summary>
/// A game event whose methods read something from <see cref="GameEventContextRegistry"/>. The generator writes
/// <see cref="AcceptContexts"/> from the event's <see cref="IGameEventRequiresContext{T}"/> interfaces.
/// </summary>
internal interface IGameEventRequiresContextBase
{
    /// <summary>Calls <see cref="IGameEventContextVisitor.Accept{T}"/> once for each type the event requires.</summary>
    void AcceptContexts(IGameEventContextVisitor visitor);
}

/// <summary>
/// The event's methods read a <typeparamref name="T"/> from <see cref="GameEventContextRegistry"/>. <typeparamref name="T"/>
/// is the service itself, never a wrapper around it.
/// </summary>
internal interface IGameEventRequiresContext<T> : IGameEventRequiresContextBase
    where T : class
{
    // empty
}
