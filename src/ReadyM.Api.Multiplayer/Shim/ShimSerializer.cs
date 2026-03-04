using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Serialization;

namespace ReadyM.Api.Multiplayer.Shim;

public class ShimSerializer
{
    private readonly JsonSerializerOptions _options;
    
    public ShimSerializer(TextRelaySerializer serializer)
    {
        var shimRecordingConverter = new ShimRecordingJsonConverter();
        var objectConverter = new PolymorphicObjectJsonConverter(serializer);
        var nullableObjectConverter = new PolymorphicNullableObjectJsonConverter(serializer);
        var shimBufferConverter = new ShimBufferConverter();

        _options = new JsonSerializerOptions()
        {
            WriteIndented = true
        };

        foreach (var converter in serializer.Converters)
        {
            _options.Converters.Add(converter);
        }
        
        _options.Converters.Add(shimRecordingConverter);
        _options.Converters.Add(objectConverter);
        _options.Converters.Add(nullableObjectConverter);
        _options.Converters.Add(shimBufferConverter);
    }
    
    public async Task<ShimRecording?> LoadAsync(Stream stream)
    {
        var s = await new StreamReader(stream).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(s))
            return null;
        return JsonSerializer.Deserialize<ShimRecording>(s, _options);
    }

    public ShimRecording? Load(Stream stream)
        => LoadAsync(stream).GetAwaiter().GetResult();

    public ShimRecording? Load(string path)
    {
        using var stream = File.OpenRead(path);
        return Load(stream);
    }
    
    public async Task<ShimRecordingMetadata?> LoadMetadataAsync(Stream stream)
    {
        var s = await new StreamReader(stream).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(s))
            return null;
        return JsonSerializer.Deserialize<ShimRecordingMetadata>(s, _options);
    }
    
    public ShimRecordingMetadata? LoadMetadata(Stream stream)
        => LoadMetadataAsync(stream).GetAwaiter().GetResult();

    public ShimRecordingMetadata? LoadMetadata(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadMetadata(stream);
    }
    
    public async Task<ShimDatabaseMetadata?> LoadDatabaseMetadataAsync(string path)
    {
        var dir = Directory.GetFiles(path);
        if (dir.Length == 0)
            return null;

        var streams = new List<FileStream>();
        var metadataTasks = new List<Task<ShimRecordingMetadata?>>();

        ushort maxPlayerIndex = 0;

        try
        {
            foreach (var file in dir)
            {
                if (!file.EndsWith(".shim.meta"))
                    continue;

                var stream = File.OpenRead(file);
                streams.Add(stream);

                var metadataTask = LoadMetadataAsync(stream);
                metadataTasks.Add(metadataTask);
            }

            await Task.WhenAll(metadataTasks);

            foreach (var task in metadataTasks)
            {
                if (task.IsFaulted)
                    continue;
                if (task.IsCanceled)
                    continue;

                var metadata = await task;
                if (metadata == null)
                    continue;

                maxPlayerIndex = Math.Max(maxPlayerIndex, metadata.PlayerId.RawValue);
            }
        }
        finally
        {
            foreach (var stream in streams)
            {
                stream.Dispose();
            }
        }

        return new ShimDatabaseMetadata()
        {
            MaxPlayerId = new PlayerId(maxPlayerIndex),
        };
    }
    
    public ShimDatabaseMetadata? LoadDatabaseMetadata(string path)
        => LoadDatabaseMetadataAsync(path).GetAwaiter().GetResult();
    
    public void Save(ShimRecording recording, Stream stream)
    {
        using var writer = new StreamWriter(stream);
        var options = _options;
        lock (recording)
        {
            writer.Write(JsonSerializer.Serialize(recording, options));
        }
    }
    
    public void Save(ShimRecording recording, string path)
    {
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        Save(recording, stream);
    }

    public void SaveMetadata(ShimRecordingMetadata metadata, Stream stream)
    {
        using var writer = new StreamWriter(stream);
        var options = _options;
        lock (metadata)
        {
            writer.Write(JsonSerializer.Serialize(metadata, options));
        }
    }

    public void SaveMetadata(ShimRecordingMetadata metadata, string path)
    {
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        SaveMetadata(metadata, stream);
    }
}
