using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[NativeComponent<LocallyCreatedEntityTag>]
[StructLayout(LayoutKind.Sequential)]
public readonly struct LocallyCreatedEntityTag : ITag;