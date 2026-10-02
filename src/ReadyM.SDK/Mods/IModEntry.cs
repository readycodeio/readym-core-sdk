using System.ComponentModel;

namespace ReadyM.SDK.Mods;

/// Implemented by the generated half of a [ModEntry] class.
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IModEntry
{
    void SetModDirectory(string directory);

    void Start();
}
