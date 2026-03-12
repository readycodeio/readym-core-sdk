using System;

namespace ReadyM.Api.Multiplayer.Mapping.Policies.Data.Common;

internal class FuncDataPolicy<TContext>(
    Func<TContext, bool> shouldEcsCopyToGame,
    Func<TContext, bool> canSetFromApi,
    Func<TContext, bool> shouldGameCopyToEcs,
    Func<TContext, bool> shouldSetLocally)
    : IMappingDataPolicy<TContext>
    where TContext : struct
{
    public Type ContextType
        => typeof(TContext);
    
    public bool ShouldEcsCopyToGame(in TContext context)
        => shouldEcsCopyToGame(context);

    public bool CanSetFromApi(in TContext context)
        => canSetFromApi(context);

    public bool ShouldGameCopyToEcs(in TContext context)
        => shouldGameCopyToEcs(context);

    public bool CanGameSetLocally(in TContext context)
        => shouldSetLocally(context);
}