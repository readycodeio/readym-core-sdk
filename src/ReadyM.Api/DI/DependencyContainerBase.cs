using System;
using System.Collections.Generic;
using System.Linq;
using DryIoc;
using Microsoft.Extensions.Logging;

namespace ReadyM.Api.DI;

public abstract class DependencyContainerBase : IDependencyContainer, IDisposable
{
    protected internal IContainer Container { get; private set; } = new Container(rules =>
        rules.With(FactoryMethod.ConstructorWithResolvableArguments)
            .WithDefaultReuse(Reuse.Singleton)
            .WithDefaultIfAlreadyRegistered(IfAlreadyRegistered.AppendNewImplementation)
            .WithUseInterpretation()
    );

    public virtual void Init()
    {
        Container.RegisterInstance(Container);
        Container.RegisterInstance<IDependencyContainer>(this);
        Container.Register(typeof(ILogger<>), typeof(Logger<>), ifAlreadyRegistered: IfAlreadyRegistered.Replace);
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        Container.Dispose();
    }

    public void RegisterSingleton<TService, TImplementation>(TImplementation instance, bool replace = false) where TImplementation : TService
        => Container.RegisterInstance<TService>(instance, RegistrationMode(replace));

    public T Resolve<T>() => Container.Resolve<T>();
    public IEnumerable<T> ResolveAll<T>() => Container.ResolveMany<T>();

    public void RegisterSingleton<T>(bool replace = false) => Container.Register<T>(ifAlreadyRegistered: RegistrationMode(replace));
    public void RegisterSingleton<T>(T instance, bool replace = false) => Container.RegisterInstance(instance, RegistrationMode(replace));

    public void RegisterSingleton<TService>(Type type, bool replace = false) => Container.Register(typeof(TService), type, ifAlreadyRegistered: RegistrationMode(replace));

    public void RegisterSingleton<TService, TImplementation>(bool replace = false) where TImplementation : TService
        => Container.Register<TService, TImplementation>(ifAlreadyRegistered: RegistrationMode(replace));

    // null defers to the container rules, which append rather than replace
    private static IfAlreadyRegistered? RegistrationMode(bool replace)
        => replace ? IfAlreadyRegistered.Replace : null;

    public void StartHostedServices()
    {
        Container = Container.WithNoMoreRegistrationAllowed();

        var logger = Container.Resolve<ILogger<DependencyContainerBase>>();

        var hostedServices = Container.GetServiceRegistrations()
            .Where(r => typeof(IHostedService).IsAssignableFrom(r.Factory.ImplementationType ?? r.ServiceType))
            .Where(r => r.Factory.Reuse is null or SingletonReuse)
            .OrderBy(r => r.FactoryRegistrationOrder)
            .GroupBy(r => r.FactoryRegistrationOrder, (_, r) => r.First());

        foreach (var r in hostedServices)
        {
            var service = (IHostedService)Container.Resolve(r.ServiceType, r.OptionalServiceKey);
            service.OnScopeStart();
            logger.LogDebug("Started hosted service: {ServiceType}", r.ServiceType.FullName);
        }
    }
}