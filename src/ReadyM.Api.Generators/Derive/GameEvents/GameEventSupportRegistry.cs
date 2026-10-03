using System.Collections.Generic;
using System.Linq;

namespace ReadyM.Api.Generators.Derive.GameEvents;

/// <summary>Every discriminator the generator knows; the list is closed. An event whose policy fits none of them
/// writes its <c>IGameEvent</c> by hand.</summary>
internal static class GameEventSupportRegistry
{
    private sealed class Visitor(IReadOnlyList<IGameEventSupportImpl> impls)
        : DeriveSupportVisitorBase<GameEventModel, IGameEventSupportImpl>(impls, null)
    {
        protected override string ToDisplayString(GameEventModel item)
            => item.Event.ToDisplayString();
    }

    private static readonly IGameEventSupportImpl[] Impls =
    [
        new OwnershipBasedSupportImpl(),
        new AlwaysPropagatesSupportImpl(),
        new AlwaysPropagatesToEcsOnlySupportImpl(),
        new AlwaysPropagatesToGameOnlySupportImpl(),
        new MasterClientManagedSupportImpl(),
        new RunOnMasterClientOnlySupportImpl(),
    ];

    internal static readonly DeriveSupportVisitorBase<GameEventModel, IGameEventSupportImpl> SupportVisitor = new Visitor(Impls);

    internal static readonly HashSet<string> DiscriminatorNames =
    [
        "OwnershipBasedAttribute",
        "AlwaysPropagatesAttribute",
        "AlwaysPropagatesToEcsOnlyAttribute",
        "AlwaysPropagatesToGameOnlyAttribute",
        "MasterClientManagedAttribute",
        "RunOnMasterClientOnlyAttribute",
    ];

    internal static readonly HashSet<string> NamesNeedingSubject =
        [..Impls.OfType<GameEventSupportImplBase>().Where(i => i.NeedsSubject).Select(i => i.AttributeName)];
}
