namespace Yooni.Native.Container;

public static class ByteHashUtils
{
    public static unsafe uint GetByteHash(byte* data, int length)
    {
        // FIXME: Optimize to not go byte-by-byte but rather 4-bytes-by-4-bytes.
        const uint prime = 397;
        var hash = (uint)length;

        for (var i = 0; i < length; i++)
        {
            hash *= prime;
            hash ^= (data[i]);
        }

        return hash;
    }
}