using System.Collections.Generic;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Shim;

public class ShimRecordingMetadata
{
    public PlayerId PlayerId { get; set; }
    public List<PlayerId> Dependencies { get; set; } = new();

    public void AddDependency(PlayerId playerId)
    {
        Dependencies.Add(playerId);
    }
}
