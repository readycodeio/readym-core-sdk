using System.ComponentModel;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Saves;

/// <summary>
/// How a save names a dimension across runs. A dimension id lives one run, so a save names a persistent dimension by
/// its name, and a load turns the name back into the id it has in this run.
/// </summary>
/// <exclude />
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDimensionSaveNames
{
    /// <summary>The persistent dimension's name; false for the main dimension, a temporary one or an unknown id.</summary>
    bool TryGetPersistentName(DimensionId dimension, out string name);

    /// <summary>
    /// The id a persistent dimension of this name has in this run. One that is not live yet gets its id now, and keeps
    /// it when a mod creates it, and when it is destroyed and created again.
    /// </summary>
    DimensionId GetPersistentDimensionId(string name);
}
