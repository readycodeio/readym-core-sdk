namespace ReadyM.Api.Generators.Derive.GameEvents;

internal sealed class MasterClientManagedSupportImpl() : GameEventSupportImplBase(
    "MasterClientManagedAttribute", needsSubject: false, CSharpEmitGameEventContext.ClientState)
{
    public override void EmitCanGameEventNotifyEcsBody(CSharpEmitGameEventContext context)
        => context.Append($"{context.IsMasterClient} ? {Notify("Notify")} : {Notify("DontNotify")}");

    public override void EmitCanGameEventRunLocallyBody(CSharpEmitGameEventContext context)
        => context.Append($"{context.IsMasterClient} ? {Result("RunAll")} : {Result("Rejected")}");

    public override void EmitCanEcsInvokeGameEventBody(CSharpEmitGameEventContext context)
        => context.Append($"{context.IsMasterClient} ? {Result("DontRun")} : {Result("RunAll")}");
}
