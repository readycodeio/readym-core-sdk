using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Archetypes.Access;

public delegate TAccess CollectionAccess<out TAccess>(in EntityHandle handle)
#if NET
    where TAccess : allows ref struct
#endif
;