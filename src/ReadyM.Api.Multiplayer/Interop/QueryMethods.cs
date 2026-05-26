using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.ECS.Registry;
using Yooni.Native.Container;

namespace ReadyM.Api.Multiplayer.Interop;

public delegate NetworkedComponentId GetComponentIdByNameDelegate(NativeString256 typeName);

public delegate void EmbedForEach<T1>(ref T1 component1)
    where T1 : struct;

public delegate void EmbedQuery1(NetworkedComponentId compId, EmbedQueryDelegate1 callback);

public delegate void EmbedQueryDelegate1(Chunks1 chunks);

public delegate void HostQueryDelegate1(EmbedQueryDelegate1 callback);

// ---

public delegate void EmbedForEach<T1, T2>(ref T1 component1, ref T2 component2)
    where T1 : struct
    where T2 : struct;

public delegate void EmbedQuery2(NetworkedComponentId compId1, NetworkedComponentId compId2, EmbedQueryDelegate2 callback);

public delegate void EmbedQueryDelegate2(Chunks2 chunks);

public delegate void HostQueryDelegate2(EmbedQueryDelegate2 callback);
