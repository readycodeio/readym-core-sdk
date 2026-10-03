namespace ReadyM.Api.Mapping.Events;

/// <summary>Where <see cref="GameEventContextRegistry"/> gets each type an event requires, once, at start-up.</summary>
internal interface IGameEventContextSource
{
    T Get<T>()
        where T : class;
}
