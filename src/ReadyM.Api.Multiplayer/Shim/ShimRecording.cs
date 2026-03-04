using System.Collections.Generic;
using System.Text.Json.Serialization;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Shim;

public class ShimRecording
{
    private PlayerId? _playerId;
    private readonly List<ShimResponseItem> _responseItems;

    [JsonPropertyName("playerId")]
    public PlayerId? PlayerId
        => _playerId;
    
    [JsonPropertyName("responseItems")]
    public IReadOnlyList<ShimResponseItem> ResponseItems
        => _responseItems;

    public void SetPlayerId(PlayerId? playerId)
    {
        _playerId = playerId;
    }
    
    public ShimRecording()
    {
        _responseItems = new List<ShimResponseItem>();
    }

    public ShimRecording(ShimRecording recording)
    {
        _playerId = recording._playerId;
        _responseItems = new List<ShimResponseItem>(recording._responseItems);
    }

    public ShimRecording(ShimRecording recording, PlayerId? overridePlayerId)
    {
        _playerId = overridePlayerId;
        _responseItems = new List<ShimResponseItem>(recording._responseItems);
    }

    public ShimRecording(IEnumerable<ShimResponseItem> items, PlayerId? playerId)
    {
        _playerId = playerId;
        _responseItems = new List<ShimResponseItem>(items);
    }

    public void AddResponseItem(ShimResponseItem responseItem)
    {
        _responseItems.Add(responseItem);
    }
}
