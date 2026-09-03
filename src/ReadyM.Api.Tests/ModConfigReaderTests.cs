using System;
using System.IO;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Api.Loader;
using Xunit;

namespace ReadyM.Api.Tests;

public sealed class ModConfigReaderTests : IDisposable
{
    private sealed class Config
    {
        public int Rounds { get; set; } = 3;
        public bool AntiStall { get; set; } = true;
        public bool Cheats { get; set; }
    }

    private sealed class RequiredConfig
    {
        [JsonRequired] public string Arena { get; set; } = "";
    }

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "modconfig-" + Guid.NewGuid().ToString("N"));

    public ModConfigReaderTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private void Write(string fileName, string json) => File.WriteAllText(Path.Combine(_directory, fileName), json);

    private T Read<T>(string fileName = ModConfigReader.DefaultFileName) where T : class, new()
        => ModConfigReader.Read<T>(_directory, fileName, NullLogger.Instance);

    [Fact]
    public void MissingFileYieldsDefaults()
    {
        var config = Read<Config>();

        Assert.Equal(3, config.Rounds);
        Assert.True(config.AntiStall);
        Assert.False(config.Cheats);
    }

    [Fact]
    public void AbsentKeysKeepTheirDefaults()
    {
        Write(ModConfigReader.DefaultFileName, """{ "Rounds": 5 }""");

        var config = Read<Config>();

        Assert.Equal(5, config.Rounds);
        Assert.True(config.AntiStall);
    }

    [Fact]
    public void CommentsTrailingCommasAndCasingAreAccepted()
    {
        Write(ModConfigReader.DefaultFileName, """
            {
                // how many rounds decide a tournament
                "rounds": 7,
                "CHEATS": true,
            }
            """);

        var config = Read<Config>();

        Assert.Equal(7, config.Rounds);
        Assert.True(config.Cheats);
    }

    [Fact]
    public void UnknownKeyIsRejected()
    {
        Write(ModConfigReader.DefaultFileName, """{ "Roundz": 5 }""");

        var error = Assert.Throws<ModConfigException>(() => Read<Config>());
        Assert.Contains("Roundz", error.Message);
    }

    [Fact]
    public void MalformedJsonIsRejected()
    {
        Write(ModConfigReader.DefaultFileName, "{ not json");

        Assert.Throws<ModConfigException>(() => Read<Config>());
    }

    [Fact]
    public void MissingRequiredPropertyIsRejected()
    {
        Write(ModConfigReader.DefaultFileName, "{ }");

        var error = Assert.Throws<ModConfigException>(() => Read<RequiredConfig>());
        Assert.Contains("Arena", error.Message);
    }

    [Fact]
    public void FileNameSelectsTheFile()
    {
        Write(ModConfigReader.DefaultFileName, """{ "Rounds": 1 }""");
        Write("arenas.json", """{ "Rounds": 2 }""");

        Assert.Equal(2, Read<Config>("arenas.json").Rounds);
    }
}
