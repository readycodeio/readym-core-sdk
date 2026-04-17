using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.Interop;

public delegate void EmbedForEach<T1>(ref T1 component1)
    where T1 : struct;

public delegate void EmbedQuery(NetworkedComponentId compId, EmbedQueryDelegate callback);

public delegate void EmbedQueryDelegate(Chunks1 chunks);
public delegate void EmbedQueryDelegate<T>(Chunks1 chunks);

public delegate void HostQueryDelegate(EmbedQueryDelegate callback);
