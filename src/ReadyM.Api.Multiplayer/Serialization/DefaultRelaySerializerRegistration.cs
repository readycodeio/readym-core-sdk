using System.Collections.Generic;
using System.Numerics;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Serialization;

public class DefaultRelaySerializerRegistration : IRelaySerializerRegistration
{
    public void Register(RelaySerializer serializer)
    {
        // MUST BE registered first (code 255), hardcoded in OpaqueData dictionary deserialization
        serializer.HashtableTypeCode = serializer.RegisterType(typeof(Dictionary<object, object?>), (stream, customObject) =>
        {
            var hashtable = (Dictionary<object, object?>)customObject;

            stream.Put((ushort)hashtable.Count);

            foreach (var kvp in hashtable)
            {
                serializer.SerializeObject(stream, kvp.Key);
                serializer.SerializeObject(stream, kvp.Value);
            }
        }, stream =>
        {
            var len = stream.GetUShort();

            var hashtable = new Dictionary<object, object?>();
            for (var i = 0; i < len; i++)
            {
                var key = serializer.DeserializeObject(stream)!;
                var value = serializer.DeserializeObject(stream);

                hashtable.Add(key, value);
            }

            return hashtable;
        });

        serializer.RegisterType(typeof(PlayerId), (stream, customObject) => { stream.Put((PlayerId)customObject); }, stream => stream.Get<PlayerId>());

        serializer.RegisterType(typeof(byte), (stream, customObject) => { stream.Put((byte)customObject); }, stream => stream.GetByte());

        serializer.RegisterType(typeof(short), (stream, customObject) => { stream.Put((short)customObject); }, stream => stream.GetShort());
        serializer.RegisterType(typeof(ushort), (stream, customObject) => { stream.Put((ushort)customObject); }, stream => stream.GetUShort());

        serializer.RegisterType(typeof(int), (stream, customObject) => { stream.Put((int)customObject); }, stream => stream.GetInt());

        serializer.RegisterType(typeof(long), (stream, customObject) => { stream.Put((long)customObject); }, stream => stream.GetLong());

        serializer.RegisterType(typeof(float), (stream, customObject) => { stream.Put((float)customObject); }, stream => stream.GetFloat());

        serializer.RegisterType(typeof(double), (stream, customObject) => { stream.Put((double)customObject); }, stream => stream.GetDouble());

        serializer.RegisterType(typeof(string), (stream, customObject) => { stream.Put((string)customObject); }, stream => stream.GetString());

        serializer.RegisterType(typeof(bool), (stream, customObject) => { stream.Put((bool)customObject); }, stream => stream.GetBool());

        serializer.RegisterType(typeof(byte[]), (stream, customObject) =>
        {
            var byteArray = (byte[])customObject;
            stream.PutBytesWithLength(byteArray);
        }, stream => stream.GetBytesWithLength());

        serializer.RegisterType(typeof(int[]), (stream, customObject) =>
        {
            var intArray = (int[])customObject;
            stream.PutArray(intArray);
        }, stream => stream.GetIntArray());

        serializer.RegisterType(typeof(Vector3), (stream, customObject) =>
        {
            var vector = (Vector3)customObject;
            stream.Put(vector.X);
            stream.Put(vector.Y);
            stream.Put(vector.Z);
        }, stream =>
        {
            var x = stream.GetFloat();
            var y = stream.GetFloat();
            var z = stream.GetFloat();
            return new Vector3(x, y, z);
        });
    }
}