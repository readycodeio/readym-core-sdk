using System.ComponentModel;

namespace ReadyM.SDK.Services;

/// Encapsulates the on/off state of a service so only the SDK can change it internally.
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ServiceSwitch
{
    public bool On { get; private set; } = true;

    internal void Set(bool on) => On = on;
}