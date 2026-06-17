using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Ecs.Components;
using ReadyM.Relay.Server.Sdk.Interop;

namespace ReadyM.Relay.Server.Sdk.Ecs;

/// <summary>
/// Mod-side ECS API. Wraps the function pointers exposed by the AOT server.
/// All component types - whether defined in the server binary or in this mod - are
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

    [ThreadStatic]
    private static ChunkCallbackState _tlsState;

    private struct ChunkCallbackState
    {
        public object? Callback;
        public IntPtr State;
        public object? ManagedState;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WithCallback(object callback, Action body)
    {
        _tlsState.Callback = callback;
        try
        {
            body();
        }
        finally
        {
            _tlsState.Callback = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WithCallbackAndManagedState(object callback, object? managed, Action body)
    {
        _tlsState.Callback = callback;
        _tlsState.ManagedState = managed;
        try
        {
            body();
        }
        finally
        {
            _tlsState.Callback = null;
            _tlsState.ManagedState = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WithCallbackAndState(object callback, IntPtr statePtr, Action body)
    {
        _tlsState.Callback = callback;
        _tlsState.State = statePtr;
        try
        {
            body();
        }
        finally
        {
            _tlsState.Callback = null;
            _tlsState.State = IntPtr.Zero;
        }
    }

    #region Query 1

    public delegate void EmbedForEach<T1>(ref T1 c1)
        where T1 : struct;

    public delegate void EmbedForEachState<T, TState>(ref T component, ref TState state)
        where T : struct;

    public delegate void EmbedForEachStateManaged<T, in TState>(ref T component, TState state)
        where T : struct
        where TState : class;

    private static unsafe void IterateChunk1<T>(IntPtr d, int count, int s)
        where T : struct
    {
        var cb = (EmbedForEach<T>)_tlsState.Callback!;
        var p = (byte*)d;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T>(p + i * s));
    }

    private static unsafe void IterateChunk1State<T, TState>(IntPtr d, int count, int s)
        where T : struct where TState : unmanaged
    {
        var cb = (EmbedForEachState<T, TState>)_tlsState.Callback!;
        var sp = (TState*)_tlsState.State;
        var p = (byte*)d;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T>(p + i * s), ref *sp);
    }

    private static unsafe void IterateChunk1ManagedState<T, TState>(IntPtr d, int count, int s)
        where T : struct where TState : class
    {
        var cb = (EmbedForEachState<T, TState>)_tlsState.Callback!;
        var ms = (TState)_tlsState.ManagedState!;
        var p = (byte*)d;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T>(p + i * s), ref ms);
    }

    public void Query<T>(EmbedForEach<T> callback) where T : struct
    {
        var id = _registry.ResolveComponentId<T>();
        WithCallback(callback, () =>
            _query1(id, static (d, count, s) => IterateChunk1<T>(d, count, s)));
    }

    public void Query<T, TState>(TState state, EmbedForEachStateManaged<T, TState> callback)
        where T : struct where TState : class
    {
        var id = _registry.ResolveComponentId<T>();
        WithCallbackAndManagedState(callback, state, () =>
            _query1(id, static (d, count, s) => IterateChunk1ManagedState<T, TState>(d, count, s)));
    }

    public void Query<T, TState>(ref TState state, EmbedForEachState<T, TState> callback)
        where T : struct where TState : unmanaged
    {
        var id = _registry.ResolveComponentId<T>();
        unsafe
        {
            fixed (TState* sp = &state)
            {
                WithCallbackAndState(callback, (IntPtr)sp, () =>
                    _query1(id, static (d, count, s) => IterateChunk1State<T, TState>(d, count, s)));
            }
        }
    }

    #endregion

    #region Query 2

    public delegate void EmbedForEach<T1, T2>(ref T1 c1, ref T2 c2)
        where T1 : struct where T2 : struct;

    public delegate void EmbedForEachState<T1, T2, TState>(ref T1 c1, ref T2 c2, ref TState state)
        where T1 : struct where T2 : struct;

    public delegate void EmbedForEachStateManaged<T1, T2, in TState>(ref T1 c1, ref T2 c2, TState state)
        where T1 : struct where T2 : struct where TState : class;

    private static unsafe void IterateChunk2<T1, T2>(IntPtr d1, IntPtr d2, int count, int s1, int s2)
        where T1 : struct where T2 : struct
    {
        var cb = (EmbedForEach<T1, T2>)_tlsState.Callback!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2));
    }

    private static unsafe void IterateChunk2State<T1, T2, TState>(IntPtr d1, IntPtr d2, int count, int s1, int s2)
        where T1 : struct where T2 : struct where TState : unmanaged
    {
        var cb = (EmbedForEachState<T1, T2, TState>)_tlsState.Callback!;
        var sp = (TState*)_tlsState.State;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref *sp);
    }

    private static unsafe void IterateChunk2ManagedState<T1, T2, TState>(IntPtr d1, IntPtr d2, int count, int s1, int s2)
        where T1 : struct where T2 : struct where TState : class
    {
        var cb = (EmbedForEachState<T1, T2, TState>)_tlsState.Callback!;
        var ms = (TState)_tlsState.ManagedState!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref ms);
    }

    public void Query<T1, T2>(EmbedForEach<T1, T2> callback)
        where T1 : struct where T2 : struct
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        WithCallback(callback, () =>
            _query2(c1, c2, static (d1, d2, count, s1, s2) => IterateChunk2<T1, T2>(d1, d2, count, s1, s2)));
    }

    public void Query<T1, T2, TState>(TState state, EmbedForEachStateManaged<T1, T2, TState> callback)
        where T1 : struct where T2 : struct where TState : class
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        WithCallbackAndManagedState(callback, state, () =>
            _query2(c1, c2, static (d1, d2, count, s1, s2) => IterateChunk2ManagedState<T1, T2, TState>(d1, d2, count, s1, s2)));
    }

    public void Query<T1, T2, TState>(ref TState state, EmbedForEachState<T1, T2, TState> callback)
        where T1 : struct where T2 : struct where TState : unmanaged
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        unsafe
        {
            fixed (TState* sp = &state)
            {
                WithCallbackAndState(callback, (IntPtr)sp, () =>
                    _query2(c1, c2, static (d1, d2, count, s1, s2) => IterateChunk2State<T1, T2, TState>(d1, d2, count, s1, s2)));
            }
        }
    }

    #endregion

    #region Query 3

    public delegate void EmbedForEach<T1, T2, T3>(ref T1 c1, ref T2 c2, ref T3 c3)
        where T1 : struct where T2 : struct where T3 : struct;

    public delegate void EmbedForEachState<T1, T2, T3, TState>(ref T1 c1, ref T2 c2, ref T3 c3, ref TState state)
        where T1 : struct where T2 : struct where T3 : struct;

    public delegate void EmbedForEachStateManaged<T1, T2, T3, in TState>(ref T1 c1, ref T2 c2, ref T3 c3, TState state)
        where T1 : struct where T2 : struct where T3 : struct where TState : class;

    private static unsafe void IterateChunk3<T1, T2, T3>(IntPtr d1, IntPtr d2, IntPtr d3, int count, int s1, int s2, int s3)
        where T1 : struct where T2 : struct where T3 : struct
    {
        var cb = (EmbedForEach<T1, T2, T3>)_tlsState.Callback!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3));
    }

    private static unsafe void IterateChunk3State<T1, T2, T3, TState>(IntPtr d1, IntPtr d2, IntPtr d3, int count, int s1, int s2, int s3)
        where T1 : struct where T2 : struct where T3 : struct where TState : unmanaged
    {
        var cb = (EmbedForEachState<T1, T2, T3, TState>)_tlsState.Callback!;
        var sp = (TState*)_tlsState.State;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref *sp);
    }

    private static unsafe void IterateChunk3ManagedState<T1, T2, T3, TState>(IntPtr d1, IntPtr d2, IntPtr d3, int count, int s1, int s2, int s3)
        where T1 : struct where T2 : struct where T3 : struct where TState : class
    {
        var cb = (EmbedForEachState<T1, T2, T3, TState>)_tlsState.Callback!;
        var ms = (TState)_tlsState.ManagedState!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref ms);
    }

    public void Query<T1, T2, T3>(EmbedForEach<T1, T2, T3> callback)
        where T1 : struct where T2 : struct where T3 : struct
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        WithCallback(callback, () =>
            _query3(c1, c2, c3, static (d1, d2, d3, count, s1, s2, s3) => IterateChunk3<T1, T2, T3>(d1, d2, d3, count, s1, s2, s3)));
    }

    public void Query<T1, T2, T3, TState>(TState state, EmbedForEachStateManaged<T1, T2, T3, TState> callback)
        where T1 : struct where T2 : struct where T3 : struct where TState : class
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        WithCallbackAndManagedState(callback, state, () =>
            _query3(c1, c2, c3, static (d1, d2, d3, count, s1, s2, s3) => IterateChunk3ManagedState<T1, T2, T3, TState>(d1, d2, d3, count, s1, s2, s3)));
    }

    public void Query<T1, T2, T3, TState>(ref TState state, EmbedForEachState<T1, T2, T3, TState> callback)
        where T1 : struct where T2 : struct where T3 : struct where TState : unmanaged
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        unsafe
        {
            fixed (TState* sp = &state)
            {
                WithCallbackAndState(callback, (IntPtr)sp, () =>
                    _query3(c1, c2, c3, static (d1, d2, d3, count, s1, s2, s3) => IterateChunk3State<T1, T2, T3, TState>(d1, d2, d3, count, s1, s2, s3)));
            }
        }
    }

    #endregion

    #region Query 4

    public delegate void EmbedForEach<T1, T2, T3, T4>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct;

    public delegate void EmbedForEachState<T1, T2, T3, T4, TState>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref TState state)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct;

    public delegate void EmbedForEachStateManaged<T1, T2, T3, T4, in TState>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, TState state)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where TState : class;

    private static unsafe void IterateChunk4<T1, T2, T3, T4>(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, int count, int s1, int s2, int s3, int s4)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
    {
        var cb = (EmbedForEach<T1, T2, T3, T4>)_tlsState.Callback!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        var p4 = (byte*)d4;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref Unsafe.AsRef<T4>(p4 + i * s4));
    }

    private static unsafe void IterateChunk4State<T1, T2, T3, T4, TState>(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, int count, int s1, int s2, int s3, int s4)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where TState : unmanaged
    {
        var cb = (EmbedForEachState<T1, T2, T3, T4, TState>)_tlsState.Callback!;
        var sp = (TState*)_tlsState.State;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        var p4 = (byte*)d4;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref Unsafe.AsRef<T4>(p4 + i * s4), ref *sp);
    }

    private static unsafe void IterateChunk4ManagedState<T1, T2, T3, T4, TState>(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, int count, int s1, int s2, int s3, int s4)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where TState : class
    {
        var cb = (EmbedForEachState<T1, T2, T3, T4, TState>)_tlsState.Callback!;
        var ms = (TState)_tlsState.ManagedState!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        var p4 = (byte*)d4;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref Unsafe.AsRef<T4>(p4 + i * s4), ref ms);
    }

    public void Query<T1, T2, T3, T4>(EmbedForEach<T1, T2, T3, T4> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();
        WithCallback(callback, () =>
            _query4(c1, c2, c3, c4, static (d1, d2, d3, d4, count, s1, s2, s3, s4) => IterateChunk4<T1, T2, T3, T4>(d1, d2, d3, d4, count, s1, s2, s3, s4)));
    }

    public void Query<T1, T2, T3, T4, TState>(TState state, EmbedForEachStateManaged<T1, T2, T3, T4, TState> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where TState : class
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();
        WithCallbackAndManagedState(callback, state, () =>
            _query4(c1, c2, c3, c4, static (d1, d2, d3, d4, count, s1, s2, s3, s4) => IterateChunk4ManagedState<T1, T2, T3, T4, TState>(d1, d2, d3, d4, count, s1, s2, s3, s4)));
    }

    public void Query<T1, T2, T3, T4, TState>(ref TState state, EmbedForEachState<T1, T2, T3, T4, TState> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where TState : unmanaged
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();
        unsafe
        {
            fixed (TState* sp = &state)
            {
                WithCallbackAndState(callback, (IntPtr)sp, () =>
                    _query4(c1, c2, c3, c4, static (d1, d2, d3, d4, count, s1, s2, s3, s4) => IterateChunk4State<T1, T2, T3, T4, TState>(d1, d2, d3, d4, count, s1, s2, s3, s4)));
            }
        }
    }

    #endregion

    #region Query 5

    public delegate void EmbedForEach<T1, T2, T3, T4, T5>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct;

    public delegate void EmbedForEachState<T1, T2, T3, T4, T5, TState>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref TState state)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct;

    public delegate void EmbedForEachStateManaged<T1, T2, T3, T4, T5, in TState>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, TState state)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where TState : class;

    private static unsafe void IterateChunk5<T1, T2, T3, T4, T5>(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, IntPtr d5, int count, int s1, int s2, int s3, int s4, int s5)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
    {
        var cb = (EmbedForEach<T1, T2, T3, T4, T5>)_tlsState.Callback!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        var p4 = (byte*)d4;
        var p5 = (byte*)d5;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref Unsafe.AsRef<T4>(p4 + i * s4), ref Unsafe.AsRef<T5>(p5 + i * s5));
    }

    private static unsafe void IterateChunk5State<T1, T2, T3, T4, T5, TState>(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, IntPtr d5, int count, int s1, int s2, int s3, int s4, int s5)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where TState : unmanaged
    {
        var cb = (EmbedForEachState<T1, T2, T3, T4, T5, TState>)_tlsState.Callback!;
        var sp = (TState*)_tlsState.State;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        var p4 = (byte*)d4;
        var p5 = (byte*)d5;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref Unsafe.AsRef<T4>(p4 + i * s4), ref Unsafe.AsRef<T5>(p5 + i * s5), ref *sp);
    }

    private static unsafe void IterateChunk5ManagedState<T1, T2, T3, T4, T5, TState>(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, IntPtr d5, int count, int s1, int s2, int s3, int s4, int s5)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where TState : class
    {
        var cb = (EmbedForEachState<T1, T2, T3, T4, T5, TState>)_tlsState.Callback!;
        var ms = (TState)_tlsState.ManagedState!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        var p4 = (byte*)d4;
        var p5 = (byte*)d5;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref Unsafe.AsRef<T4>(p4 + i * s4), ref Unsafe.AsRef<T5>(p5 + i * s5), ref ms);
    }

    public void Query<T1, T2, T3, T4, T5>(EmbedForEach<T1, T2, T3, T4, T5> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();
        var c5 = _registry.ResolveComponentId<T5>();
        WithCallback(callback, () =>
            _query5(c1, c2, c3, c4, c5, static (d1, d2, d3, d4, d5, count, s1, s2, s3, s4, s5) => IterateChunk5<T1, T2, T3, T4, T5>(d1, d2, d3, d4, d5, count, s1, s2, s3, s4, s5)));
    }

    public void Query<T1, T2, T3, T4, T5, TState>(TState state, EmbedForEachStateManaged<T1, T2, T3, T4, T5, TState> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where TState : class
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();
        var c5 = _registry.ResolveComponentId<T5>();
        WithCallbackAndManagedState(callback, state, () =>
            _query5(c1, c2, c3, c4, c5, static (d1, d2, d3, d4, d5, count, s1, s2, s3, s4, s5) => IterateChunk5ManagedState<T1, T2, T3, T4, T5, TState>(d1, d2, d3, d4, d5, count, s1, s2, s3, s4, s5)));
    }

    public void Query<T1, T2, T3, T4, T5, TState>(ref TState state, EmbedForEachState<T1, T2, T3, T4, T5, TState> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where TState : unmanaged
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();
        var c5 = _registry.ResolveComponentId<T5>();
        unsafe
        {
            fixed (TState* sp = &state)
            {
                WithCallbackAndState(callback, (IntPtr)sp, () =>
                    _query5(c1, c2, c3, c4, c5, static (d1, d2, d3, d4, d5, count, s1, s2, s3, s4, s5) => IterateChunk5State<T1, T2, T3, T4, T5, TState>(d1, d2, d3, d4, d5, count, s1, s2, s3, s4, s5)));
            }
        }
    }

    #endregion

    #region Query 6

    public delegate void EmbedForEach<T1, T2, T3, T4, T5, T6>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct;

    public delegate void EmbedForEachState<T1, T2, T3, T4, T5, T6, TState>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref TState state)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct;

    public delegate void EmbedForEachStateManaged<T1, T2, T3, T4, T5, T6, in TState>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, TState state)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where TState : class;

    private static unsafe void IterateChunk6<T1, T2, T3, T4, T5, T6>(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, IntPtr d5, IntPtr d6, int count, int s1, int s2, int s3, int s4, int s5, int s6)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
    {
        var cb = (EmbedForEach<T1, T2, T3, T4, T5, T6>)_tlsState.Callback!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        var p4 = (byte*)d4;
        var p5 = (byte*)d5;
        var p6 = (byte*)d6;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref Unsafe.AsRef<T4>(p4 + i * s4), ref Unsafe.AsRef<T5>(p5 + i * s5), ref Unsafe.AsRef<T6>(p6 + i * s6));
    }

    private static unsafe void IterateChunk6State<T1, T2, T3, T4, T5, T6, TState>(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, IntPtr d5, IntPtr d6, int count, int s1, int s2, int s3, int s4, int s5, int s6)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where TState : unmanaged
    {
        var cb = (EmbedForEachState<T1, T2, T3, T4, T5, T6, TState>)_tlsState.Callback!;
        var sp = (TState*)_tlsState.State;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        var p4 = (byte*)d4;
        var p5 = (byte*)d5;
        var p6 = (byte*)d6;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref Unsafe.AsRef<T4>(p4 + i * s4), ref Unsafe.AsRef<T5>(p5 + i * s5), ref Unsafe.AsRef<T6>(p6 + i * s6), ref *sp);
    }

    private static unsafe void IterateChunk6ManagedState<T1, T2, T3, T4, T5, T6, TState>(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, IntPtr d5, IntPtr d6, int count, int s1, int s2, int s3, int s4, int s5, int s6)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where TState : class
    {
        var cb = (EmbedForEachState<T1, T2, T3, T4, T5, T6, TState>)_tlsState.Callback!;
        var ms = (TState)_tlsState.ManagedState!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        var p4 = (byte*)d4;
        var p5 = (byte*)d5;
        var p6 = (byte*)d6;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref Unsafe.AsRef<T4>(p4 + i * s4), ref Unsafe.AsRef<T5>(p5 + i * s5), ref Unsafe.AsRef<T6>(p6 + i * s6), ref ms);
    }

    public void Query<T1, T2, T3, T4, T5, T6>(EmbedForEach<T1, T2, T3, T4, T5, T6> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();
        var c5 = _registry.ResolveComponentId<T5>();
        var c6 = _registry.ResolveComponentId<T6>();
        WithCallback(callback, () =>
            _query6(c1, c2, c3, c4, c5, c6, static (d1, d2, d3, d4, d5, d6, count, s1, s2, s3, s4, s5, s6) => IterateChunk6<T1, T2, T3, T4, T5, T6>(d1, d2, d3, d4, d5, d6, count, s1, s2, s3, s4, s5, s6)));
    }

    public void Query<T1, T2, T3, T4, T5, T6, TState>(TState state, EmbedForEachStateManaged<T1, T2, T3, T4, T5, T6, TState> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where TState : class
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();
        var c5 = _registry.ResolveComponentId<T5>();
        var c6 = _registry.ResolveComponentId<T6>();
        WithCallbackAndManagedState(callback, state, () =>
            _query6(c1, c2, c3, c4, c5, c6, static (d1, d2, d3, d4, d5, d6, count, s1, s2, s3, s4, s5, s6) => IterateChunk6ManagedState<T1, T2, T3, T4, T5, T6, TState>(d1, d2, d3, d4, d5, d6, count, s1, s2, s3, s4, s5, s6)));
    }

    public void Query<T1, T2, T3, T4, T5, T6, TState>(ref TState state, EmbedForEachState<T1, T2, T3, T4, T5, T6, TState> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where TState : unmanaged
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();
        var c5 = _registry.ResolveComponentId<T5>();
        var c6 = _registry.ResolveComponentId<T6>();
        unsafe
        {
            fixed (TState* sp = &state)
            {
                WithCallbackAndState(callback, (IntPtr)sp, () =>
                    _query6(c1, c2, c3, c4, c5, c6, static (d1, d2, d3, d4, d5, d6, count, s1, s2, s3, s4, s5, s6) => IterateChunk6State<T1, T2, T3, T4, T5, T6, TState>(d1, d2, d3, d4, d5, d6, count, s1, s2, s3, s4, s5, s6)));
            }
        }
    }

    #endregion
}