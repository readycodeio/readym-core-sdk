using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ReadyM.Api.Loader;

/// <summary>
/// Finds mod packages under a mods directory and orders them so every mod loads after its dependencies.
/// </summary>
internal sealed class ModPackageLocator(ILogger logger)
{
    public IReadOnlyList<ModPackage> Locate(string modsDirectory)
    {
        if (!Directory.Exists(modsDirectory))
        {
            logger.LogError("Mods directory '{ModsDirectory}' not found", modsDirectory);
            return [];
        }

        var packages = new List<ModPackage>();
        var seen = new Dictionary<string, ModPackage>(StringComparer.Ordinal);

        foreach (var directory in Directory.GetDirectories(modsDirectory).OrderBy(d => d, StringComparer.Ordinal))
        {
            if (!TryReadManifest(directory, out var manifest))
                continue;

            if (seen.TryGetValue(manifest.UniqueId, out var existing))
            {
                logger.LogError("Mod '{Directory}' declares id {Id}, already used by '{Other}'. Skipping",
                    directory, manifest.UniqueId, existing.Directory);
                continue;
            }

            var package = new ModPackage(directory, manifest);
            seen[manifest.UniqueId] = package;
            packages.Add(package);
        }

        return Sort(packages, seen);
    }

    private bool TryReadManifest(string directory, out ModManifest manifest)
    {
        manifest = null!;
        var path = Path.Combine(directory, ModManifest.FileName);

        if (!File.Exists(path))
        {
            // Not a mod package. The mods folder also holds client-only folders such as ReflectionOnly.
            logger.LogDebug("'{Directory}' has no {FileName}, not a mod package", directory, ModManifest.FileName);
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize(File.ReadAllText(path), ModManifestSerializerContext.Default.ModManifest);

            if (parsed == null || string.IsNullOrWhiteSpace(parsed.UniqueId))
            {
                logger.LogError("Mod '{Directory}' has an invalid {FileName}. Skipping", directory, ModManifest.FileName);
                return false;
            }

            manifest = parsed;
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to parse {FileName} in '{Directory}'. Skipping", ModManifest.FileName, directory);
            return false;
        }
    }

    private List<ModPackage> Sort(List<ModPackage> packages, Dictionary<string, ModPackage> byId)
    {
        var sorted = new List<ModPackage>(packages.Count);
        var resolved = new Dictionary<string, bool>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);

        foreach (var package in packages)
        {
            Visit(package, byId, sorted, resolved, visiting);
        }

        logger.LogInformation("Mod load order: {Order}",
            string.Join(", ", sorted.Select(p => $"{p.Manifest.UniqueId} v{p.Manifest.Version}")));

        return sorted;
    }

    private bool Visit(
        ModPackage package,
        Dictionary<string, ModPackage> byId,
        List<ModPackage> sorted,
        Dictionary<string, bool> resolved,
        HashSet<string> visiting)
    {
        var id = package.Manifest.UniqueId;

        if (resolved.TryGetValue(id, out var alreadyOk))
            return alreadyOk;

        if (!visiting.Add(id))
        {
            logger.LogError("Circular mod dependency involving '{Id}'. Skipping", id);
            return false;
        }

        var ok = true;

        foreach (var dependency in package.Manifest.Dependencies)
        {
            if (!byId.TryGetValue(dependency.UniqueId, out var dependencyPackage))
            {
                logger.LogError("Mod '{Id}' depends on '{DependencyId}', which is not installed. Skipping",
                    id, dependency.UniqueId);
                ok = false;
                break;
            }

            if (!IsVersionSatisfied(dependencyPackage.Manifest.Version, dependency.MinimumVersion))
            {
                logger.LogError("Mod '{Id}' needs '{DependencyId}' {MinimumVersion} or newer, found {FoundVersion}. Skipping",
                    id, dependency.UniqueId, dependency.MinimumVersion, dependencyPackage.Manifest.Version);
                ok = false;
                break;
            }

            if (!Visit(dependencyPackage, byId, sorted, resolved, visiting))
            {
                logger.LogError("Mod '{Id}' is skipped because its dependency '{DependencyId}' could not be loaded",
                    id, dependency.UniqueId);
                ok = false;
                break;
            }
        }

        visiting.Remove(id);
        resolved[id] = ok;

        if (ok)
            sorted.Add(package);

        return ok;
    }

    private static bool IsVersionSatisfied(string available, string minimum)
    {
        if (string.IsNullOrWhiteSpace(minimum))
            return true;

        if (Version.TryParse(available, out var availableVersion) && Version.TryParse(minimum, out var minimumVersion))
            return availableVersion >= minimumVersion;

        return string.Compare(available, minimum, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
