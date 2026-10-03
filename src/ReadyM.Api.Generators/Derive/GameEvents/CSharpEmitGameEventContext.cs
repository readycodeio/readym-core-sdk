using System.Text;

namespace ReadyM.Api.Generators.Derive.GameEvents;

/// <summary>The expressions emitted game event code is built from: result values, the required types, the subject.</summary>
internal sealed class CSharpEmitGameEventContext(StringBuilder sb, GameEventModel model)
{
    private const string EventsNamespace = "global::ReadyM.Api.Mapping.Events";
    public const string OwnershipManager = "global::ReadyM.Relay.Client.State.ClientOwnershipManager";
    public const string ClientState = "global::ReadyM.Relay.Client.State.ClientState";

    public const string ContextsParameter = "contexts";

    public GameEventModel Model { get; } = model;

    public string OwnsSubject
        => $"{ContextsParameter}.GetContext<{OwnershipManager}>().OwnsEntity(this.{Model.Subject!.Name})";

    public string IsMasterClient
        => $"{ContextsParameter}.GetContext<{ClientState}>().IsMasterClient";

    public static string Result(string name)
        => $"{EventsNamespace}.GameEventResult.{name}";

    public static string Notify(string name)
        => $"{EventsNamespace}.GameEventNotifyResult.{name}";

    public void Append(string code)
        => sb.Append(code);
}
