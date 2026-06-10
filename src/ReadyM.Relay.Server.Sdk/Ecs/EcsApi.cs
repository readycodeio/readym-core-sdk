using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Ecs.Components;
using ReadyM.Relay.Server.Sdk.Interop;

namespace ReadyM.Relay.Server.Sdk.Ecs;

/// <summary>
/// Plugin-side ECS API. Wraps the function pointers exposed by the AOT server.
/// All component types - whether defined in the server binary or in this plugin - are
/// identified by <c>int</c> component IDs assigned at registration time.
/// </summary>
public class EcsApi
{
    private readonly Query1Delegate _query1;
    private readonly Query2Delegate _query2;
    private readonly Query3Delegate _query3;
    private readonly Query4Delegate _query4;
    private readonly Query5Delegate _query5;
    private readonly Query6Delegate _query6;
    private readonly CreateNetworkedEntityDelegate _createNetworkedEntity;
    private readonly GetComponentPointerDelegate _getComponentPointer;
    private readonly ComponentRegistry _registry;

    /// <summary>
    /// Plugin-side ECS API. Wraps the function pointers exposed by the AOT server.
    /// All component types - whether defined in the server binary or in this plugin - are
    /// identified by <c>int</c> component IDs assigned at registration time.
    /// </summary>
    internal EcsApi(EcsApiPointers pointers, ComponentRegistry registry)
    {
        _registry = registry;
        _query1 = Marshal.GetDelegateForFunctionPointer<Query1Delegate>(pointers.Query1);
        _query2 = Marshal.GetDelegateForFunctionPointer<Query2Delegate>(pointers.Query2);
        _query3 = Marshal.GetDelegateForFunctionPointer<Query3Delegate>(pointers.Query3);
        _query4 = Marshal.GetDelegateForFunctionPointer<Query4Delegate>(pointers.Query4);
        _query5 = Marshal.GetDelegateForFunctionPointer<Query5Delegate>(pointers.Query5);
        _query6 = Marshal.GetDelegateForFunctionPointer<Query6Delegate>(pointers.Query6);
        _createNetworkedEntity = Marshal.GetDelegateForFunctionPointer<CreateNetworkedEntityDelegate>(pointers.CreateNetworkedEntity);
        _getComponentPointer = Marshal.GetDelegateForFunctionPointer<GetComponentPointerDelegate>(pointers.GetComponentPointer);
    }

    public Entity CreateEntity(ArchetypeId archetypeId)
    {
        return new Entity(_createNetworkedEntity(archetypeId), _getComponentPointer, _registry);
    }

    public void Query<T>(EmbedForEach<T> callback) where T : struct
    {
        var id = _registry.ResolveComponentId<T>();
        _query1(id, (data, count, stride) =>
        {
            unsafe
            {
                var ptr = (byte*)data;
                for (var i = 0; i < count; i++)
                    callback(ref Unsafe.AsRef<T>(ptr + i * stride));
            }
        });
    }

    public void Query<T1, T2>(EmbedForEach<T1, T2> callback)
        where T1 : struct where T2 : struct
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();

        _query2(c1, c2, (d1, d2, count, s1, s2) =>
        {
            unsafe
            {
                var p1 = (byte*)d1;
                var p2 = (byte*)d2;
                for (var i = 0; i < count; i++)
                    callback(ref Unsafe.AsRef<T1>(p1 + i * s1),
                        ref Unsafe.AsRef<T2>(p2 + i * s2));
            }
        });
    }

    public void Query<T1, T2, T3>(EmbedForEach<T1, T2, T3> callback)
        where T1 : struct where T2 : struct where T3 : struct
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();

        _query3(c1, c2, c3, (d1, d2, d3, count, s1, s2, s3) =>
        {
            unsafe
            {
                var p1 = (byte*)d1;
                var p2 = (byte*)d2;
                var p3 = (byte*)d3;
                for (var i = 0; i < count; i++)
                    callback(ref Unsafe.AsRef<T1>(p1 + i * s1),
                        ref Unsafe.AsRef<T2>(p2 + i * s2),
                        ref Unsafe.AsRef<T3>(p3 + i * s3));
            }
        });
    }

    public void Query<T1, T2, T3, T4>(EmbedForEach<T1, T2, T3, T4> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();

        _query4(c1, c2, c3, c4, (d1, d2, d3, d4, count, s1, s2, s3, s4) =>
        {
            unsafe
            {
                var p1 = (byte*)d1;
                var p2 = (byte*)d2;
                var p3 = (byte*)d3;
                var p4 = (byte*)d4;
                for (var i = 0; i < count; i++)
                    callback(ref Unsafe.AsRef<T1>(p1 + i * s1),
                        ref Unsafe.AsRef<T2>(p2 + i * s2),
                        ref Unsafe.AsRef<T3>(p3 + i * s3),
                        ref Unsafe.AsRef<T4>(p4 + i * s4));
            }
        });
    }

    public void Query<T1, T2, T3, T4, T5>(EmbedForEach<T1, T2, T3, T4, T5> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();
        var c5 = _registry.ResolveComponentId<T5>();

        _query5(c1, c2, c3, c4, c5, (d1, d2, d3, d4, d5, count, s1, s2, s3, s4, s5) =>
        {
            unsafe
            {
                var p1 = (byte*)d1;
                var p2 = (byte*)d2;
                var p3 = (byte*)d3;
                var p4 = (byte*)d4;
                var p5 = (byte*)d5;
                for (var i = 0; i < count; i++)
                    callback(ref Unsafe.AsRef<T1>(p1 + i * s1),
                        ref Unsafe.AsRef<T2>(p2 + i * s2),
                        ref Unsafe.AsRef<T3>(p3 + i * s3),
                        ref Unsafe.AsRef<T4>(p4 + i * s4),
                        ref Unsafe.AsRef<T5>(p5 + i * s5));
            }
        });
    }

    public void Query<T1, T2, T3, T4, T5, T6>(EmbedForEach<T1, T2, T3, T4, T5, T6> callback)
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
        where T5 : struct
        where T6 : struct
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();
        var c5 = _registry.ResolveComponentId<T5>();
        var c6 = _registry.ResolveComponentId<T6>();

        _query6(c1, c2, c3, c4, c5, c6,
            (d1, d2, d3, d4, d5, d6, count, s1, s2, s3, s4, s5, s6) =>
            {
                unsafe
                {
                    var p1 = (byte*)d1;
                    var p2 = (byte*)d2;
                    var p3 = (byte*)d3;
                    var p4 = (byte*)d4;
                    var p5 = (byte*)d5;
                    var p6 = (byte*)d6;
                    for (var i = 0; i < count; i++)
                        callback(ref Unsafe.AsRef<T1>(p1 + i * s1),
                            ref Unsafe.AsRef<T2>(p2 + i * s2),
                            ref Unsafe.AsRef<T3>(p3 + i * s3),
                            ref Unsafe.AsRef<T4>(p4 + i * s4),
                            ref Unsafe.AsRef<T5>(p5 + i * s5),
                            ref Unsafe.AsRef<T6>(p6 + i * s6));
                }
            });
    }
}