using System.ComponentModel;

namespace ReadyM.SDK.Services;

/// What every [Service] compiles down to. Implemented by generated code.
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IService
{
    ServiceSwitch Switch { get; }

    /// Runs the service's own OnEnabled or OnDisabled, once the switch has moved.
    void Switched(bool enabled);
}
