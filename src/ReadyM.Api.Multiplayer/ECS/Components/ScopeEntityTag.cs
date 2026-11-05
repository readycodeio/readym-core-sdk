using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Attributes;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[NativeComponent]
[StructLayout(LayoutKind.Sequential)]
internal readonly struct ScopeEntityTag : ITag;
