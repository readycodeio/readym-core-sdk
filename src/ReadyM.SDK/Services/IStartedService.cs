using System.ComponentModel;

namespace ReadyM.SDK.Services;

/// What a [Service] declaring a Start or an OnEnabled compiles down to.
/// Implemented by generated code.
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IStartedService
{
    /// Runs the service's own Start and its first OnEnabled.
    void Start();
}
