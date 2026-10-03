namespace ReadyM.Api.Mapping.Events;

/// <summary>
/// Whether the mapped event manager sends a game event on to the ECS. Answered by the event, asked by the manager.
/// </summary>
public enum GameEventNotifyResult : byte
{
    DontNotify = 0,
    Notify = 1,
}
