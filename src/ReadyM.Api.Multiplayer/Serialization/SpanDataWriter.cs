using System;
using LiteNetLib.Utils;

namespace ReadyM.Api.Multiplayer.Serialization;

public class SpanDataWriter : NetDataWriter
{
    public SpanDataWriter() : base(true) { }
    public SpanDataWriter(int initialSize) : base(true, initialSize) { }

    public void PutSpan(ReadOnlySpan<byte> data)
    {
        ResizeIfNeed(_position + data.Length);

        data.CopyTo(new Span<byte>(_data, _position, data.Length));
        _position += data.Length;
    }

    public void PutSpanWithSize(ReadOnlySpan<byte> data)
    {
        Put((ushort)data.Length);

        ResizeIfNeed(_position + data.Length);

        data.CopyTo(new Span<byte>(_data, _position, data.Length));
        _position += data.Length;
    }
}