using System;
using System.Collections.Generic;
using System.ComponentModel;
using ReadyM.Api.DI;

namespace ReadyM.SDK.Commands;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class CommandProviders
{
    private static readonly List<Type> Seen = new();
    private static readonly List<Func<IDependencyContainer, ICommandProvider>> Resolvers = new();

    public static IReadOnlyList<Type> Types => Seen;

    internal static IReadOnlyList<Func<IDependencyContainer, ICommandProvider>> All => Resolvers;

    public static void Hold<TProvider>() where TProvider : class, ICommandProvider
    {
        if (Seen.Contains(typeof(TProvider))) return;

        Seen.Add(typeof(TProvider));
        Resolvers.Add(container => container.Resolve<TProvider>());
    }
}
