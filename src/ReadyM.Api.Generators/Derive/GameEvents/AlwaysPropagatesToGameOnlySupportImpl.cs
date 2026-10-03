namespace ReadyM.Api.Generators.Derive.GameEvents;

internal sealed class AlwaysPropagatesToGameOnlySupportImpl() : GameEventSupportImplBase("AlwaysPropagatesToGameOnlyAttribute", needsSubject: false)
{
    public override void EmitCanGameEventNotifyEcsBody(CSharpEmitGameEventContext context)
        => context.Append(Notify("DontNotify"));

    public override void EmitCanGameEventRunLocallyBody(CSharpEmitGameEventContext context)
        => context.Append(Result("RunAll"));

    public override void EmitCanEcsInvokeGameEventBody(CSharpEmitGameEventContext context)
        => context.Append(Result("RunAll"));
}
