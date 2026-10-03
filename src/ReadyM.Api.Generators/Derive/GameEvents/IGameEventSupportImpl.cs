using System.Collections.Generic;

namespace ReadyM.Api.Generators.Derive.GameEvents;

/// <summary>Emits one discriminator's answers: an expression for each of the three <c>IGameEvent</c> methods.</summary>
internal interface IGameEventSupportImpl : IDeriveSupportImplBase<GameEventModel>
{
    bool NeedsSubject { get; }

    /// <summary>The types the emitted methods read from the context registry, fully qualified.</summary>
    IReadOnlyList<string> RequiredContexts { get; }

    void EmitCanGameEventNotifyEcsBody(CSharpEmitGameEventContext context);
    void EmitCanGameEventRunLocallyBody(CSharpEmitGameEventContext context);
    void EmitCanEcsInvokeGameEventBody(CSharpEmitGameEventContext context);
}
