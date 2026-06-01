using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Interop;
using Yooni.Native.Container;

/// <summary>
/// Plugin-side ECS API. Wraps the function pointers exposed by the AOT server.
/// All component types - whether defined in the server binary or in this plugin - are
/// identified by <c>int</c> component IDs assigned at registration time.
/// </summary>
public class EcsApi(EcsApiPointers pointers)
{
    private readonly GetComponentIdByNameDelegate _getComponentIdByName =
        Marshal.GetDelegateForFunctionPointer<GetComponentIdByNameDelegate>(pointers.GetComponentIdByName);

    private readonly RegisterPluginComponentDelegate _registerPluginComponent =
        Marshal.GetDelegateForFunctionPointer<RegisterPluginComponentDelegate>(pointers.RegisterPluginComponent);

    private readonly Query1Delegate _query1 = Marshal.GetDelegateForFunctionPointer<Query1Delegate>(pointers.Query1);
    private readonly Query2Delegate _query2 = Marshal.GetDelegateForFunctionPointer<Query2Delegate>(pointers.Query2);
    private readonly Query3Delegate _query3 = Marshal.GetDelegateForFunctionPointer<Query3Delegate>(pointers.Query3);
    private readonly Query4Delegate _query4 = Marshal.GetDelegateForFunctionPointer<Query4Delegate>(pointers.Query4);
    private readonly Query5Delegate _query5 = Marshal.GetDelegateForFunctionPointer<Query5Delegate>(pointers.Query5);
    private readonly Query6Delegate _query6 = Marshal.GetDelegateForFunctionPointer<Query6Delegate>(pointers.Query6);

    // Maps plugin struct type → component ID assigned by the server registry.
    private readonly Dictionary<Type, (int ComponentId, int Stride)> _registered = new();

    // -------------------------------------------------------------------------
    // Registration
    // -------------------------------------------------------------------------

    /// <summary>
    /// Registers a plugin-defined component type with the server ECS.
    /// Must be called during <c>ServerModBase.Init()</c>, before any entity creation.
    /// Returns the component ID to use in all subsequent <c>Query</c> calls.
    /// </summary>
    public int RegisterComponent<T>() where T : unmanaged
    {
        var type   = typeof(T);
        var stride = Unsafe.SizeOf<T>();

        if (_registered.ContainsKey(type))
            throw new InvalidOperationException($"{type.FullName} is already registered.");

        if (stride > 256)
            throw new ArgumentException(
                $"{type.Name} is {stride} bytes which exceeds the 256-byte maximum.");

        var id = _registerPluginComponent(stride);

        if (id < 0)
            throw new InvalidOperationException(
                $"Server refused to register {type.Name}: component slot limit reached.");

        _registered[type] = (id, stride);
        return id;
    }

    // -------------------------------------------------------------------------
    // Query - 1 component
    // -------------------------------------------------------------------------

    public void Query<T>(int c1, EmbedForEach<T> callback) where T : struct
    {
        _query1(c1, (data, count, stride) =>
        {
            unsafe
            {
                var ptr = (byte*)data;
                for (var i = 0; i < count; i++)
                    callback(ref Unsafe.AsRef<T>(ptr + i * stride));
            }
        });
    }

    // -------------------------------------------------------------------------
    // Query - 2 components
    // -------------------------------------------------------------------------

    public void Query<T1, T2>(int c1, int c2, EmbedForEach<T1, T2> callback)
        where T1 : struct where T2 : struct
    {
        _query2(c1, c2, (d1, d2, count, s1, s2) =>
        {
            unsafe
            {
                var p1 = (byte*)d1; var p2 = (byte*)d2;
                for (var i = 0; i < count; i++)
                    callback(ref Unsafe.AsRef<T1>(p1 + i * s1),
                             ref Unsafe.AsRef<T2>(p2 + i * s2));
            }
        });
    }

    // -------------------------------------------------------------------------
    // Query - 3 components
    // -------------------------------------------------------------------------

    public void Query<T1, T2, T3>(int c1, int c2, int c3, EmbedForEach<T1, T2, T3> callback)
        where T1 : struct where T2 : struct where T3 : struct
    {
        _query3(c1, c2, c3, (d1, d2, d3, count, s1, s2, s3) =>
        {
            unsafe
            {
                var p1 = (byte*)d1; var p2 = (byte*)d2; var p3 = (byte*)d3;
                for (var i = 0; i < count; i++)
                    callback(ref Unsafe.AsRef<T1>(p1 + i * s1),
                             ref Unsafe.AsRef<T2>(p2 + i * s2),
                             ref Unsafe.AsRef<T3>(p3 + i * s3));
            }
        });
    }

    // -------------------------------------------------------------------------
    // Query - 4 components
    // -------------------------------------------------------------------------

    public void Query<T1, T2, T3, T4>(int c1, int c2, int c3, int c4,
        EmbedForEach<T1, T2, T3, T4> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
    {
        _query4(c1, c2, c3, c4, (d1, d2, d3, d4, count, s1, s2, s3, s4) =>
        {
            unsafe
            {
                var p1 = (byte*)d1; var p2 = (byte*)d2;
                var p3 = (byte*)d3; var p4 = (byte*)d4;
                for (var i = 0; i < count; i++)
                    callback(ref Unsafe.AsRef<T1>(p1 + i * s1),
                             ref Unsafe.AsRef<T2>(p2 + i * s2),
                             ref Unsafe.AsRef<T3>(p3 + i * s3),
                             ref Unsafe.AsRef<T4>(p4 + i * s4));
            }
        });
    }

    // -------------------------------------------------------------------------
    // Query - 5 components
    // -------------------------------------------------------------------------

    public void Query<T1, T2, T3, T4, T5>(int c1, int c2, int c3, int c4, int c5,
        EmbedForEach<T1, T2, T3, T4, T5> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
    {
        _query5(c1, c2, c3, c4, c5, (d1, d2, d3, d4, d5, count, s1, s2, s3, s4, s5) =>
        {
            unsafe
            {
                var p1 = (byte*)d1; var p2 = (byte*)d2; var p3 = (byte*)d3;
                var p4 = (byte*)d4; var p5 = (byte*)d5;
                for (var i = 0; i < count; i++)
                    callback(ref Unsafe.AsRef<T1>(p1 + i * s1),
                             ref Unsafe.AsRef<T2>(p2 + i * s2),
                             ref Unsafe.AsRef<T3>(p3 + i * s3),
                             ref Unsafe.AsRef<T4>(p4 + i * s4),
                             ref Unsafe.AsRef<T5>(p5 + i * s5));
            }
        });
    }

    // -------------------------------------------------------------------------
    // Query - 6 components
    // -------------------------------------------------------------------------

    public void Query<T1, T2, T3, T4, T5, T6>(int c1, int c2, int c3, int c4, int c5, int c6,
        EmbedForEach<T1, T2, T3, T4, T5, T6> callback)
        where T1 : struct where T2 : struct where T3 : struct
        where T4 : struct where T5 : struct where T6 : struct
    {
        _query6(c1, c2, c3, c4, c5, c6,
            (d1, d2, d3, d4, d5, d6, count, s1, s2, s3, s4, s5, s6) =>
            {
                unsafe
                {
                    var p1 = (byte*)d1; var p2 = (byte*)d2; var p3 = (byte*)d3;
                    var p4 = (byte*)d4; var p5 = (byte*)d5; var p6 = (byte*)d6;
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

    // -------------------------------------------------------------------------
    // Legacy - kept for server-defined networked components where the plugin
    // receives the component ID via a well-known constant rather than calling
    // RegisterComponent. These IDs come from the server's NetworkedComponentRegistry.
    // -------------------------------------------------------------------------

    public NetworkedComponentId GetNetworkComponentId<T>() where T : struct
        => _getComponentIdByName(new NativeString256(typeof(T).FullName, false));
}