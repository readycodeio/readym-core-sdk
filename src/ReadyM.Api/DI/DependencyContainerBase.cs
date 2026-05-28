using System;
using System.Linq;
using DryIoc;
using Microsoft.Extensions.Logging;

namespace ReadyM.Api.DI;

public abstract class DependencyContainerBase : IDependencyContainer, IDisposable
{
    protected internal IContainer Container { get; private set; } = new Container(rules =>
        rules.With(FactoryMethod.ConstructorWithResolvableArguments)
            .WithDefaultReuse(Reuse.Singleton)
            .WithUseInterpretation()
    );

    public virtual void Init()
    {
        Container.RegisterInstance<IDependencyContainer>(this);
        Container.Register(typeof(ILogger<>), typeof(Logger<>), ifAlreadyRegistered: IfAlreadyRegistered.Replace);
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        Container.Dispose();
    }

    public void RegisterSingleton<TService, TImplementation>(TImplementation instance) where TImplementation : TService
        => Container.RegisterInstance<TService>(instance);

    public T Resolve<T>() => Container.Resolve<T>();

    public void RegisterSingleton<T>() => Container.Register<T>(ifAlreadyRegistered: IfAlreadyRegistered.Replace);
    public void RegisterSingleton<T>(T instance) => Container.RegisterInstance(instance, ifAlreadyRegistered: IfAlreadyRegistered.Replace);

    public void RegisterSingleton<TService>(Type type) => Container.Register(typeof(TService), type, ifAlreadyRegistered: IfAlreadyRegistered.Replace);

    public void RegisterSingleton<TService, TImplementation>() where TImplementation : TService
        => Container.Register<TService, TImplementation>(ifAlreadyRegistered: IfAlreadyRegistered.Replace);

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