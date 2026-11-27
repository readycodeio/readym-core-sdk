using System.ComponentModel;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Events;

/// <summary>What the Common project generates beside an event struct.</summary>
/// <exclude />
[EditorBrowsable(EditorBrowsableState.Never)]
public interface INetworkedEventContract<T> where T : struct, INetworkedEvent
{
    /// <summary>This event's code on the wire.</summary>
    RelayMessageCode Code { get; }

    /// <summary>Who receives it once it is accepted.</summary>
    EventScope Scope { get; }

    /// <summary>Who may raise it.</summary>
    Policy Policy { get; }

    /// <summary>The entity it is about, which the owner policy checks and a scoped audience falls back on.</summary>
    EntityHandle? Subject(in T networkEvent);

    /// <summary>The area it names with <c>[Area]</c>, if any.</summary>
    bool TryArea(in T networkEvent, out FullAreaId area);

    /// <summary>The cell it names with <c>[Cell]</c>, if any.</summary>
    bool TryCell(in T networkEvent, out FullCellId cell);

    /// <summary>The one player a <see cref="Attributes.EventScope.Single"/> event goes to.</summary>
    PlayerId Target(in T networkEvent);

    /// <summary>Reads the event, failing if it names an entity this end does not have.</summary>
    bool TryRead(INetworkedEventEntityReader entities, NetDataReader reader, out T networkEvent);

    /// <summary>Writes a refusal's reason, of the type the declaration named, or its absence.</summary>
    void WriteReason(NetDataWriter writer, object? reason);

    /// <summary>Reads that reason back, or null where the refusal carried none.</summary>
    object? ReadReason(NetDataReader reader);
}
