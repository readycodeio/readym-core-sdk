using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Serialization;
using ReadyM.Api.Multiplayer.Shim;

namespace ReadyM.Relay.Client.Shim;

public class ShimController
{
    private readonly ShimRelayRecorder? _shimRecorder;
    private readonly TextRelaySerializer _textSerializer;
    private readonly ILogger _logger;

    public ShimController(ShimRelayRecorder? shimRecorder, TextRelaySerializer textSerializer, ILogger logger)
    {
        _shimRecorder = shimRecorder;
        _textSerializer = textSerializer;
        _logger = logger;
    }

    public void Save(string recordShimFile)
    {
        if (_shimRecorder == null)
        {
            _logger.LogError("Shim recorder is not initialized. Cannot save recording.");
            return;
        }

        var shimSerializer = new ShimSerializer(_textSerializer);
        var recording = _shimRecorder.GetRecording();

        _logger.LogInformation("Saving shim recording to: {Path}", recordShimFile);
        shimSerializer.Save(recording, recordShimFile);
    }
}