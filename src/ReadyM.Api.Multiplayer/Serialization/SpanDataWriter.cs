using System;
using LiteNetLib.Utils;

namespace ReadyM.Api.Multiplayer.Serialization;

public class SpanDataWriter : NetDataWriter
{
    public SpanDataWriter() { }
    public SpanDataWriter(bool autoResize, int initialSize) : base(autoResize, initialSize) { }

    public void PutSpan(ReadOnlySpan<byte> data)
    {
        if (_autoResize)
            ResizeIfNeed(_position + data.Length);

        data.CopyTo(new Span<byte>(_data, _position, data.Length));
        _position += data.Length;
    }

    public void PutSpanWithSize(ReadOnlySpan<byte> data)
    {
        Put((ushort)data.Length);

        if (_autoResize)
            ResizeIfNeed(_position + data.Length);

        data.CopyTo(new Span<byte>(_data, _position, data.Length));
        _position += data.Length;
    }
}