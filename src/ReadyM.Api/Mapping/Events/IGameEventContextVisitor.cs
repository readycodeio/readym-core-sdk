namespace ReadyM.Api.Mapping.Events;

/// <summary>Receives the types a game event requires, one call per type.</summary>
internal interface IGameEventContextVisitor
{
    void Accept<T>()
        where T : class;
}
