using System;

namespace ReadyM.Api.DI;

/// <summary>
/// Dependency injection container interface for registering and resolving services.
/// </summary>
public interface IDependencyContainer
{
    void RegisterSingleton<TService>();
    void RegisterSingleton<TService>(TService instance);
    void RegisterSingleton<TService>(Type implementationType);
    void RegisterSingleton<TService, TImplementation>() where TImplementation : TService;
    void RegisterSingleton<TService, TImplementation>(TImplementation instance) where TImplementation : TService;   
    T Resolve<T>();
}