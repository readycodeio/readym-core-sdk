namespace ReadyM.SDK.Services;

/// Switches services on and off while the game runs.
public interface IServices
{
    /// Lets its update run again and calls its OnEnabled. Warns and does nothing if already enabled.
    void Enable<TService>() where TService : class, IService;

    /// Stops its update running and calls its OnDisabled. Warns and does nothing if already disabled.
    void Disable<TService>() where TService : class, IService;

    bool IsEnabled<TService>() where TService : class, IService;
}
