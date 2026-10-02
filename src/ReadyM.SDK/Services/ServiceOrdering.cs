using System.ComponentModel;

namespace ReadyM.SDK.Services;

/// <summary>
/// Puts services in the order their updates run.
/// </summary>
/// <remarks>
/// Kahn's algorithm with the ready set kept sorted: Before and After decide which services are
/// available at each step, and the priority only chooses among those, so a constraint always wins
/// over a priority. Equal priorities fall back to the type name, so there is always a deterministic order for a given set of mods.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceOrdering
{
    /// <param name="onUnknown">
    /// Ran for a Before or After naming a service that is not updating here, which is a mod that
    /// was not installed. The constraint is dropped.
    /// </param>
    /// <exception cref="ServiceOrderException">The constraints contain a cycle.</exception>
    public static List<T> Sort<T>(
        IReadOnlyList<T> services,
        Func<T, Type> typeOf,
        Func<T, ServiceOrder> orderOf,
        Action<Type, Type>? onUnknown = null)
    {
        var byType = new Dictionary<Type, T>();

        foreach (var service in services)
            byType[typeOf(service)] = service;

        var after = new Dictionary<Type, List<Type>>();
        var blocking = new Dictionary<Type, int>();

        foreach (var service in services)
        {
            after[typeOf(service)] = [];
            blocking[typeOf(service)] = 0;
        }

        foreach (var service in services)
        {
            var self = typeOf(service);
            var order = orderOf(service);

            foreach (var target in order.Before)
                Edge(self, target, named: target, by: self);

            foreach (var target in order.After)
                Edge(target, self, named: target, by: self);
        }

        // Everything nothing is waiting on, which is where the walk starts.
        var ready = new List<Type>();

        foreach (var pair in blocking)
            if (pair.Value == 0)
                ready.Add(pair.Key);

        var sorted = new List<T>(services.Count);

        while (ready.Count > 0)
        {
            ready.Sort(Compare);

            var next = ready[0];

            ready.RemoveAt(0);
            sorted.Add(byType[next]);

            foreach (var waiting in after[next])
                if (--blocking[waiting] == 0)
                    ready.Add(waiting);
        }

        if (sorted.Count != services.Count)
            throw new ServiceOrderException(Cycle(sorted));

        return sorted;

        void Edge(Type earlier, Type later, Type named, Type by)
        {
            // A service that is not updating here cannot be ordered against. The one that named it
            // is told so it can carry on, because a mod naming another mod's service is ordinary.
            if (!byType.ContainsKey(named))
            {
                onUnknown?.Invoke(named, by);
                return;
            }

            after[earlier].Add(later);
            blocking[later]++;
        }

        int Compare(Type left, Type right)
        {
            var byPriority = orderOf(byType[left]).Priority.CompareTo(orderOf(byType[right]).Priority);

            return byPriority != 0
                ? byPriority
                : string.CompareOrdinal(left.FullName, right.FullName);
        }

        string Cycle(List<T> emitted)
        {
            var left = new HashSet<Type>();

            foreach (var service in services)
                left.Add(typeOf(service));

            foreach (var service in emitted)
                left.Remove(typeOf(service));

            // Walking forward from anything left over has to come back on itself, since every one
            // of them is still waiting on another one of them.
            var walk = new List<Type>();
            var seen = new HashSet<Type>();
            var at = left.First();

            while (seen.Add(at))
            {
                walk.Add(at);
                at = after[at].First(left.Contains);
            }

            var from = walk.IndexOf(at);
            var loop = walk.GetRange(from, walk.Count - from).Select(type => type.Name);

            return string.Join(" -> ", loop) + " -> " + at.Name;
        }
    }
}

/// What services asked for cannot be done, because the order would have to come back on itself.
public sealed class ServiceOrderException(string cycle)
    : InvalidOperationException(
        $"Services order their updates in a cycle, so there is no order that satisfies them: {cycle}. "
        + "Remove one of the [UpdateOrder] constraints in that loop.")
{
    /// The loop, as the services in it.
    public string Cycle { get; } = cycle;
}
