using System.ComponentModel;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Archetypes;

/// What a shape or a service asked to run at a moment in an entity's life.
[EditorBrowsable(EditorBrowsableState.Never)]
public delegate void EntityHandler(in EntityHandle handle);
