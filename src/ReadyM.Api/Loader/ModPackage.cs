using System.IO;

namespace ReadyM.Api.Loader;

/// <summary>
/// A mod folder: <c>mods/&lt;name&gt;/</c> holding a manifest and a <c>client</c> and/or <c>server</c> subfolder.
/// </summary>
internal sealed class ModPackage(string directory, ModManifest manifest)
{
    public const string ClientFolder = "client";
    public const string ServerFolder = "server";

    public string Directory { get; } = directory;
    public ModManifest Manifest { get; } = manifest;

    public string FolderName => Path.GetFileName(Directory);
    public string ClientDirectory => Path.Combine(Directory, ClientFolder);
    public string ServerDirectory => Path.Combine(Directory, ServerFolder);

    public bool HasClientSide => System.IO.Directory.Exists(ClientDirectory);
    public bool HasServerSide => System.IO.Directory.Exists(ServerDirectory);
}
