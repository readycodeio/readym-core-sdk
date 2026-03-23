using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[NativeComponent<ScopeEntityTag>]
[StructLayout(LayoutKind.Sequential)]
public readonly struct ScopeEntityTag : ITag;
