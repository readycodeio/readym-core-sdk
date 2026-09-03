using System.Collections.Generic;

namespace ReadyM.Api.Loader;

internal class ModManifest
{
    public const string FileName = "manifest.json";

    public class ModDependency
    {
        public string UniqueId { get; set; } = null!;

        public string MinimumVersion { get; set; } = null!;
    }

    public string UniqueId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Version { get; set; } = null!;

    public string Author { get; set; } = null!;

    public string? Link { get; set; }

    public string? Description { get; set; }

    public List<ModDependency> Dependencies { get; set; } = [];
}
