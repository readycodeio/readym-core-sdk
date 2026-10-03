using DryIoc;
using ReadyM.Api.Mapping.Events;

namespace ReadyM.Api.DI;

/// <summary>Gives <see cref="GameEventContextRegistry"/> the types events require from the application's container.</summary>
internal sealed class ResolverGameEventContextSource(IResolver resolver) : IGameEventContextSource
{
    public T Get<T>()
        where T : class
        => resolver.Resolve<T>();
}
