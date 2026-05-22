using ReadyM.Api.Helpers;

namespace ReadyM.Api.Mapping.Policies.Data;

internal abstract class MappingDataPolicyBase<TField, TContext>(DataSideChannel sideChannel) : IMappingDataPolicy<TContext>
{
    public bool ShouldGameCopyToEcs(in TContext context)
    {
        if (sideChannel.HasData<PropagatingToGameScope<TField>>())
            return false;

        return ShouldGameCopyToEcsImpl(context);
    }

    protected abstract bool ShouldGameCopyToEcsImpl(in TContext context);

    public bool ShouldEcsCopyToGame(in TContext context)
    {
        if (sideChannel.HasData<PropagatingToEcsScope<TField>>())
            return false;

        return ShouldEcsCopyToGameImpl(context);
    }

    public abstract bool CanSetFromApi(in TContext context);

    protected abstract bool ShouldEcsCopyToGameImpl(in TContext context);

    public bool CanGameSetLocally(in TContext context)
    {
        if (sideChannel.HasData<PropagatingToGameScope<TField>>())
            return true;

        return CanGameSetLocallyImpl(context);
    }

    protected abstract bool CanGameSetLocallyImpl(in TContext context);
}