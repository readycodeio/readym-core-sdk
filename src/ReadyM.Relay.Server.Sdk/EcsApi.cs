using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Interop;
using Yooni.Native.Container;

namespace ReadyM.Relay.Server.Sdk;

public class EcsApi(EcsApiPointers pointers)
{
    private readonly GetComponentIdByNameDelegate _getComponentIdByName = Marshal.GetDelegateForFunctionPointer<GetComponentIdByNameDelegate>(pointers.GetComponentIdByName);
    private readonly EmbedQuery1 _query = Marshal.GetDelegateForFunctionPointer<EmbedQuery1>(pointers.EmbedQuery1);
    private readonly EmbedQuery2 _query2 = Marshal.GetDelegateForFunctionPointer<EmbedQuery2>(pointers.EmbedQuery2);

    public void Query<T>(EmbedForEach<T> callback)
        where T : struct
    {
        _query(GetNetworkComponent<T>(), chunks =>
        {
            var span = chunks.AsSpan<T>();
            foreach (ref var x in span)
            {
                callback(ref x);
            }
        });
    }

    public void Query<T1, T2>(EmbedForEach<T1, T2> callback)
        where T1 : struct
        where T2 : struct
    {
        _query2(GetNetworkComponent<T1>(), GetNetworkComponent<T2>(), chunks =>
        {
            var span1 = chunks.AsSpan1<T1>();
            var span2 = chunks.AsSpan2<T2>();

            for (var ix = 0; ix < span1.Length; ix++)
            {
                callback(ref span1[ix], ref span2[ix]);
            }
        });
    }

    private NetworkedComponentId GetNetworkComponent<T>()
        where T : struct
    {
        return _getComponentIdByName(new NativeString256(typeof(T).FullName, false));
    }
}