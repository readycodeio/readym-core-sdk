using System;
using Friflo.Engine.ECS.Systems;
using LiteNetLib.Utils;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.ECS.Systems;

public abstract class SendComponentDeltaSystemBase<T>(NetworkedComponentId componentId) : QuerySystem<NetworkIdComponent, T> where T : struct, INetworkedComponent
{
    protected abstract int GetMaxPacketSize();
    protected abstract void Send(NetDataWriter data);
    protected abstract bool OwnsEntity(NetworkIdComponent netId);

    private const int HeaderSize = 2; // SystemEvent byte + componentId byte

    private NetDataWriter MakeHeader()
    {
        var writer = new NetDataWriter();
        writer.Put((byte)SystemEvent.EcsUpdate);
        writer.Put(componentId);
        return writer;
    }

    protected override void OnUpdate()
    {
        var writer = MakeHeader();

        Query.ForEachEntity((ref netId, ref comp, _) =>
        {
            if (!OwnsEntity(netId))
            {
                // Skip entities not owned by this peer
                return;
            }

            var retried = false;

            while (true)
            {
                var beforeApplyPosition = writer.Length;

                if (!comp.IsDirty)
                    return;

                writer.Put(netId);

                comp.WriteDelta(writer);

                if (writer.Length > GetMaxPacketSize())
                {
                    if (retried)
                    {
                        // if we retried and still failed, log an error
                        throw new Exception("Packet too large, unable to send");
                    }

                    // Rewind and send the partial packet
                    writer.SetPosition(beforeApplyPosition);
                    Send(writer);

                    // Start a new writer and retry
                    writer = MakeHeader();
                    retried = true;

                    // Continue loop to retry
                    continue;
                }

                comp.ClearDirty();

                break;
            }
        });

        if (writer.Length > HeaderSize)
        {
            Send(writer);
        }
    }
}