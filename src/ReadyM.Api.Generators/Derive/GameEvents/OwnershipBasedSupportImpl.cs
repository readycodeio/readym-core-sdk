namespace ReadyM.Api.Generators.Derive.GameEvents;

internal sealed class OwnershipBasedSupportImpl() : GameEventSupportImplBase(
    "OwnershipBasedAttribute", needsSubject: true, CSharpEmitGameEventContext.OwnershipManager)
{
    public override void EmitCanGameEventNotifyEcsBody(CSharpEmitGameEventContext context)
        => context.Append($"{context.OwnsSubject} ? {Notify("Notify")} : {Notify("DontNotify")}");

    public override void EmitCanGameEventRunLocallyBody(CSharpEmitGameEventContext context)
        => context.Append($"{context.OwnsSubject} ? {Result("RunAll")} : {Result("Rejected")}");

    public override void EmitCanEcsInvokeGameEventBody(CSharpEmitGameEventContext context)
        => context.Append($"{context.OwnsSubject} ? {Result("DontRun")} : {Result("RunAll")}");
}
