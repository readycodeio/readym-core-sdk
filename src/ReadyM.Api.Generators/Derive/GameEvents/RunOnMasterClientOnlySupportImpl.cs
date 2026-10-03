namespace ReadyM.Api.Generators.Derive.GameEvents;

internal sealed class RunOnMasterClientOnlySupportImpl() : GameEventSupportImplBase(
    "RunOnMasterClientOnlyAttribute", needsSubject: true, CSharpEmitGameEventContext.OwnershipManager, CSharpEmitGameEventContext.ClientState)
{
    public override void EmitCanGameEventNotifyEcsBody(CSharpEmitGameEventContext context)
        => context.Append($"!{context.IsMasterClient} && {context.OwnsSubject} ? {Notify("Notify")} : {Notify("DontNotify")}");

    public override void EmitCanGameEventRunLocallyBody(CSharpEmitGameEventContext context)
        => context.Append($"{context.IsMasterClient} ? {Result("RunAll")} : {context.OwnsSubject} ? {Result("DontRun")} : {Result("Rejected")}");

    public override void EmitCanEcsInvokeGameEventBody(CSharpEmitGameEventContext context)
        => context.Append($"{context.IsMasterClient} ? {Result("RunAll")} : {Result("DontRun")}");
}
