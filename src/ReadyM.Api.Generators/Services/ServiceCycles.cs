using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Services;

/// Finds update constraints that come back on themselves, the same way the SDK does at load.
internal static class ServiceCycles
{
    /// One diagnostic per service in a loop, so the editor marks every one of them.
    public static IEnumerable<Diagnostic> Find(IReadOnlyList<INamedTypeSymbol> services)
    {
        var known = new HashSet<INamedTypeSymbol>(services, SymbolEqualityComparer.Default);
        var edges = new Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>(SymbolEqualityComparer.Default);

        foreach (var service in services)
            edges[service] = [];

        foreach (var service in services)
        {
            var order = ServiceShape.Read(service).Order;

            // Only what is in this compilation can take part: a target elsewhere cannot close a
            // loop, since that would need the two assemblies to reference each other.
            foreach (var target in order.Before.Where(known.Contains))
                edges[service].Add(target);

            foreach (var target in order.After.Where(known.Contains))
                edges[target].Add(service);
        }

        var settled = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var reported = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var found = new List<Diagnostic>();

        foreach (var service in services)
            Walk(service, []);

        return found;

        void Walk(INamedTypeSymbol at, List<INamedTypeSymbol> path)
        {
            if (settled.Contains(at))
                return;

            var loopsAt = path.FindIndex(step => SymbolEqualityComparer.Default.Equals(step, at));

            if (loopsAt >= 0)
            {
                Report(path.GetRange(loopsAt, path.Count - loopsAt), at);
                return;
            }

            path.Add(at);

            foreach (var next in edges[at])
                Walk(next, path);

            path.RemoveAt(path.Count - 1);
            settled.Add(at);
        }

        void Report(List<INamedTypeSymbol> loop, INamedTypeSymbol closes)
        {
            var named = string.Join(" -> ", loop.Select(step => step.Name)) + " -> " + closes.Name;

            foreach (var step in loop.Where(reported.Add))
                found.Add(Diagnostic.Create(
                    ServiceShape.OrdersInACycle,
                    step.Locations.FirstOrDefault() ?? Location.None,
                    step.Name,
                    named));
        }
    }
}
