using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;

namespace ReadyM.SDK.Services;

/// The IServices a container hands out. It reaches a service the same way anything else does.
internal sealed class ServiceSwitchboard(IDependencyContainer container) : IServices
{
    public void Enable<TService>() where TService : class, IService => Switch<TService>(true);

    public void Disable<TService>() where TService : class, IService => Switch<TService>(false);

    public bool IsEnabled<TService>() where TService : class, IService
        => container.Resolve<TService>().Switch.On;

    private void Switch<TService>(bool enabled) where TService : class, IService
    {
        var service = container.Resolve<TService>();

        if (service.Switch.On == enabled)
        {
            // Saying it twice is one mod disagreeing with another, or with itself. Worth seeing,
            // not worth bringing the game down over.
            container.Resolve<ILogger>().LogWarning(
                "{Service} is already {State}, so nothing was done.",
                typeof(TService).Name,
                enabled ? "enabled" : "disabled");

            return;
        }

        service.Switch.Set(enabled);
        service.Switched(enabled);
    }
}
