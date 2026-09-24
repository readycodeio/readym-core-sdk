using System.ComponentModel;

namespace ReadyM.SDK.Services;

/// What a [Service] declaring an update compiles down to. Implemented by generated code.
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IUpdatingService
{
    void Update(in UpdateTime time);
}
