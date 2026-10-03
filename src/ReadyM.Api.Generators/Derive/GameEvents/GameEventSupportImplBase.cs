using System.Collections.Generic;

namespace ReadyM.Api.Generators.Derive.GameEvents;

internal abstract class GameEventSupportImplBase(string attributeName, bool needsSubject, params string[] requiredContexts)
    : IGameEventSupportImpl
{
    public string AttributeName { get; } = attributeName;
    public bool NeedsSubject { get; } = needsSubject;
    public IReadOnlyList<string> RequiredContexts { get; } = requiredContexts;

    public bool Supports(GameEventModel model)
        => model.DiscriminatorName == attributeName;

    public abstract void EmitCanGameEventNotifyEcsBody(CSharpEmitGameEventContext context);
    public abstract void EmitCanGameEventRunLocallyBody(CSharpEmitGameEventContext context);
    public abstract void EmitCanEcsInvokeGameEventBody(CSharpEmitGameEventContext context);

    protected static string Result(string name)
        => CSharpEmitGameEventContext.Result(name);

    protected static string Notify(string name)
        => CSharpEmitGameEventContext.Notify(name);
}
