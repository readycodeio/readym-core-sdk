using System;
using System.Collections.Generic;

namespace ReadyM.Api.DI;

/// <summary>
/// Dependency injection container interface for registering and resolving services.
/// </summary>
/// <remarks>
/// Registrations are additive by default, so several mods can each contribute an
/// implementation of the same service and all of them are returned by
/// <see cref="ResolveAll{T}"/>. Pass <c>replace: true</c> to take over a service
/// another mod or the SDK already registered, which drops every existing
/// registration of that service type.
/// </remarks>
public interface IDependencyContainer
{
    void RegisterSingleton<TService>(bool replace = false);
    void RegisterSingleton<TService>(TService instance, bool replace = false);
    void RegisterSingleton<TService>(Type implementationType, bool replace = false);
    void RegisterSingleton<TService, TImplementation>(bool replace = false) where TImplementation : TService;
    void RegisterSingleton<TService, TImplementation>(TImplementation instance, bool replace = false) where TImplementation : TService;
    T Resolve<T>();
    IEnumerable<T> ResolveAll<T>();
}
