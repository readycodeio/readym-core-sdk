namespace ReadyM.Api.Generators.Derive.GameEvents;

internal sealed class AlwaysPropagatesSupportImpl() : GameEventSupportImplBase("AlwaysPropagatesAttribute", needsSubject: false)
{
    public override void EmitCanGameEventNotifyEcsBody(CSharpEmitGameEventContext context)
        => context.Append(Notify("Notify"));

    public override void EmitCanGameEventRunLocallyBody(CSharpEmitGameEventContext context)
        => context.Append(Result("RunAll"));

    public override void EmitCanEcsInvokeGameEventBody(CSharpEmitGameEventContext context)
        => context.Append(Result("RunAll"));
}
