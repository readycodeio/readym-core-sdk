using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using ReadyM.Api.DI;

namespace ReadyM.Api.Multiplayer.RPC;

/// Which half of an RPC a class implements, which decides the container it belongs in.
[EditorBrowsable(EditorBrowsableState.Never)]
public enum RpcSide
{
    Client,
    Server
}

/// Holds the [RpcHandlersFor] classes a loaded assembly declared.
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RpcHandlerRegistry
{
    private static readonly ConcurrentDictionary<Type, Declaration> Declared = new();

    /// <summary>Called by generated code for a class carrying [RpcHandlersFor].</summary>
    public static void Declare<THandlers>(RpcSide side) where THandlers : class
        => Declared[typeof(THandlers)] =
            new Declaration(side, container => container.RegisterSingleton<THandlers>());

    /// <summary>Registers every class declared for one side as a singleton, the way a [Service] is.</summary>
    public static void RegisterAll(IDependencyContainer container, RpcSide side)
    {
        foreach (var declaration in Declared.Values)
            if (declaration.Side == side)
                declaration.Register(container);
    }

    private readonly struct Declaration(RpcSide side, Action<IDependencyContainer> register)
    {
        public RpcSide Side { get; } = side;

        public Action<IDependencyContainer> Register { get; } = register;
    }
}
