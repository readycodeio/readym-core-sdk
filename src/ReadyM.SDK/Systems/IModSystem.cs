using System.ComponentModel;

namespace ReadyM.SDK.Systems;

/// What a [System] compiles down to. Implemented by generated code.
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IModSystem
{
    void Update(in Tick tick);
}
