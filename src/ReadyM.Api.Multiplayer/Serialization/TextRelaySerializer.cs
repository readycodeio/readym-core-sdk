using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ReadyM.Api.Helpers;

namespace ReadyM.Api.Multiplayer.Serialization;

public class TextRelaySerializer
{
    private readonly Dictionary<Type, string> _polymorphicByType = new();
    private readonly Dictionary<string, Type> _polymorphicByDiscriminator = new();
    private readonly List<JsonConverter> _converters = new();

    public ReadOnlyDictionary<Type, string> PolymorphicByType
        => new(_polymorphicByType);
    
    public ReadOnlyDictionary<string, Type> PolymorphicByDiscriminator
        => new(_polymorphicByDiscriminator);
    
    public ReadOnlyList<JsonConverter> Converters
        => new(_converters);

    public TextRelaySerializer(IEnumerable<ITextRelaySerializerRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            registration.Register(this);
        }
    }

    public void RegisterConverter(JsonConverter converter)
        => _converters.Add(converter);
    
    public void RegisterConverter<T>(
        TextSerializeMethod<T> serializeMethod,
        TextDeserializeMethod<T> deserializeMethod
    )
        => _converters.Add(new FuncJsonConverter<T>(serializeMethod, deserializeMethod));

    public void RegisterPolymorphicType(Type type, string discriminator)
    {
        if (_polymorphicByType.TryGetValue(type, out var value))
        {
            throw new InvalidOperationException($"Type {type} is already registered with discriminator {value}");
        }
        
        _polymorphicByType[type] = discriminator;
        _polymorphicByDiscriminator[discriminator] = type;
    }
    
    public void RegisterPolymorphicType<T>(string discriminator)
        => RegisterPolymorphicType(typeof(T), discriminator);

    public void RegisterPolymorphicType<T>(
        string discriminator,
        TextSerializeMethod<T> serializeMethod,
        TextDeserializeMethod<T> deserializeMethod
    )
    {
        RegisterPolymorphicType<T>(discriminator);
        RegisterConverter(serializeMethod, deserializeMethod);
    }
}