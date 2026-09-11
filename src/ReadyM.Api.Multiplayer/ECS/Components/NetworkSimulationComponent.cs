using System.Runtime.InteropServices;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[StructLayout(LayoutKind.Sequential)]
public struct NetworkSimulationComponent : IComponent
{
    public int MinLatency;
    public int MaxLatency;
    public int PacketLossPercent;
}