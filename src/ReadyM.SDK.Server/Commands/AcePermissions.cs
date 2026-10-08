using System.Text.Json;
using Microsoft.Extensions.Logging;
using ReadyM.SDK.Commands;

namespace ReadyM.SDK.Server.Commands;

public sealed class AcePermissions : ICommandPermissions, IDisposable
{
    private static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(250);

    private readonly string _file;
    private readonly ILogger _logger;
    private readonly FileSystemWatcher? _watcher;
    private readonly Timer _debounce;
    private volatile AceTable _table = AceTable.Empty;
    private readonly object _gate = new();

    private readonly AceDocument _fromCode = new();
    private AceDocument _fromFile = new();

    public AcePermissions(string file, ILogger logger)
    {
        _file = file;
        _logger = logger;
        _debounce = new Timer(_ => Reload(), null, Timeout.Infinite, Timeout.Infinite);

        Reload();

        var directory = Path.GetDirectoryName(file);
        if (string.IsNullOrEmpty(directory)) return;

        _watcher = new FileSystemWatcher(directory, Path.GetFileName(file))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };

        // One save raises several of these, hence the debounce.
        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Renamed += OnChanged;
        _watcher.Deleted += OnChanged;
    }

    // A caller with no ReadyM id holds nothing, there is nobody to have granted anything to
    public bool IsAllowed(Guid? caller, string permission)
        => caller is { } id && _table.IsAllowed(id.ToString("D"), permission);

    public void Allow(string principal, string permission) => Add(principal, permission, allow: true);

    public void Deny(string principal, string permission) => Add(principal, permission, allow: false);

    public void AddToGroup(string principal, string group)
    {
        lock (_gate)
        {
            var into = Guid.TryParse(principal, out _) ? _fromCode.Principals : _fromCode.Inherits;

            if (!into.TryGetValue(principal, out var groups)) into[principal] = groups = [];
            if (!groups.Contains(group)) groups.Add(group);

            Rebuild();
        }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _debounce.Dispose();
    }

    private void Add(string principal, string permission, bool allow)
    {
        lock (_gate)
        {
            _fromCode.Aces.Add(new AceEntry { Principal = principal, Object = permission, Allow = allow });
            Rebuild();
        }
    }

    private void Rebuild()
    {
        var merged = new AceDocument();

        foreach (var source in new[] { _fromCode, _fromFile })
        {
            foreach (var pair in source.Principals) merged.Principals[pair.Key] = [.. pair.Value];
            foreach (var pair in source.Inherits) merged.Inherits[pair.Key] = [.. pair.Value];

            merged.Aces.AddRange(source.Aces);
        }

        _table = new AceTable(merged);
    }

    private void OnChanged(object sender, FileSystemEventArgs e) => _debounce.Change(Settle, Timeout.InfiniteTimeSpan);

    private void Reload()
    {
        AceDocument? document;
        try
        {
            document = AceFile.Read(_file);
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            _logger.LogError(e, "{File} was not readable, so the permissions already loaded stay in force.", _file);
            return;
        }

        if (document is null)
        {
            _logger.LogInformation("No {File}, so only what mods granted in code is in force.", _file);
            lock (_gate)
            {
                _fromFile = new AceDocument();
                Rebuild();
            }

            return;
        }

        lock (_gate)
        {
            _fromFile = document;
            Rebuild();
        }

        _logger.LogInformation("Loaded {Aces} permission entries from {File}.", document.Aces.Count, _file);
    }
}
