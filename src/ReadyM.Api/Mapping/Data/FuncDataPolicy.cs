using System;

namespace ReadyM.Api.Mapping.Data;

public class FuncDataPolicy<TContext>(
    Func<TContext, bool> shouldEcsCopyToGame,
    Func<TContext, bool> shouldGameCopyToEcs,
    Func<TContext, bool> shouldSetLocally)
    : IMappingDataPolicy<TContext>
    where TContext : struct
{
    public Type ContextType
        => typeof(TContext);
    
    public bool ShouldEcsCopyToGame(in TContext context)
        => shouldEcsCopyToGame(context);

    public bool ShouldGameCopyToEcs(in TContext context)
        => shouldGameCopyToEcs(context);

    public bool ShouldGameSetLocally(in TContext context)
        => shouldSetLocally(context);
}