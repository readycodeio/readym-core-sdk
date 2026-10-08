using System.Collections.Immutable;

namespace ReadyM.SDK.Server.Commands;

public sealed class AceTable
{
    public static AceTable Empty { get; } = new(new AceDocument());

    private readonly AceDocument _document;

    public AceTable(AceDocument document) => _document = document;

    public bool IsAllowed(string principal, string permission)
    {
        var principals = Closure(principal);
        var allowed = false;

        foreach (var ace in _document.Aces)
        {
            if (!principals.Contains(ace.Principal)) continue;
            if (!Covers(ace.Object, permission)) continue;

            if (!ace.Allow) return false;

            allowed = true;
        }

        return allowed;
    }

    private ImmutableHashSet<string> Closure(string principal)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { principal };
        var pending = new Queue<string>();

        if (_document.Principals.TryGetValue(principal, out var groups))
        {
            foreach (var group in groups)
                if (seen.Add(group)) pending.Enqueue(group);
        }

        while (pending.Count > 0)
        {
            var group = pending.Dequeue();
            if (!_document.Inherits.TryGetValue(group, out var inherited)) continue;

            foreach (var next in inherited)
                if (seen.Add(next)) pending.Enqueue(next);
        }

        return seen.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool Covers(string aceObject, string permission)
        => permission.Equals(aceObject, StringComparison.OrdinalIgnoreCase)
           || (permission.Length > aceObject.Length
               && permission[aceObject.Length] == '.'
               && permission.StartsWith(aceObject, StringComparison.OrdinalIgnoreCase));
}
