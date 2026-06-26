using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace ReadyM.Api;

internal class IpcHelper(ILogger logger)
{
    private static readonly HashSet<string> RedactedKeys = ["JWT_TOKEN"];

    public Dictionary<string, string> ReadAndDeleteIpcHandshakeFile()
    {
        var tempDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ReadyM.Launcher");
        var filePath = Path.Combine(tempDir, "wukong_handshake.env");

        if (!File.Exists(filePath))
        {
            logger.LogError("Handshake file not found at {Path}. Launch the game from the ReadyM Launcher.", filePath);
            return [];
        }

        logger.LogInformation("Reading handshake file: {FilePath}", filePath);
        var lines = File.ReadAllLines(filePath);
        var data = new Dictionary<string, string>();

        // format is .env KEY=VALUE
        var regex = new Regex(@"^(?<key>[^=]+)=(?<value>.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        foreach (var line in lines)
        {
            var match = regex.Match(line);
            if (match.Success)
            {
                var key = match.Groups["key"].Value.Trim();
                var value = match.Groups["value"].Value.Trim();
                data[key] = value;

                if (RedactedKeys.Contains(key))
                {
                    logger.LogInformation("Parsed {Key}=<redacted>", key);
                }
                else
                {
                    logger.LogInformation("Parsed {Key}={Value}", key, value);
                }
            }
            else
            {
                logger.LogError("Failed to parse line: {Line}", line);
            }
        }

        // delete the file after reading
        try
        {
            File.Delete(filePath);
            logger.LogInformation("Deleted handshake file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete handshake file: {FilePath}", filePath);
        }

        return data;
    }
}