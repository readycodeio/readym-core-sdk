using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Archetypes.Access;

public delegate TAccess CollectionAccess<out TAccess>(in EntityHandle handle)
#if NET10_0_OR_GREATER
    where TAccess : allows ref struct
#endif
;