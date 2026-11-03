using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Events;
using Xunit;

namespace ReadyM.SDK.Tests.Events;

/// <summary>
/// The order two mods' handlers for one event run in. A mod says what it must follow, and everything else runs
/// in the order of the mods' own names, which is the same order on every machine.
/// </summary>
public class NetworkedEventOrderingTests
{
    private static List<string> Sorted(params (string Mod, string[] After)[] mods)
        => NetworkedEventCoordinatorBase<Tick>.SortByAfter([.. mods.Select(m => new Handlers(m.Mod, m.After))])
            .Select(handlers => handlers.Mod)
            .ToList();

    private struct Tick : INetworkedEvent
    {
        public RelayMessageCode Code => (RelayMessageCode)ushort.MaxValue;

        public void Serialize(NetDataWriter writer)
        {
        }
    }

    private sealed class Handlers(string mod, string[] after) : INetworkedEventHandlers<Tick>
    {
        public string Mod => mod;

        public IReadOnlyList<string> After => after;

        public void Run(Stage stage, ref Tick networkEvent, EventContext context)
        {
        }
    }

    /// <summary>
    /// Not the order they were handed over in. The server and a client load a different set of mods in a
    /// different sequence, so load order is two orders and the sides would apply one event differently.
    /// </summary>
    [Fact]
    public void Mods_that_say_nothing_run_in_name_order()
        => Assert.Equal(["A", "B", "C"], Sorted(("C", []), ("A", []), ("B", [])));

    [Fact]
    public void A_mod_runs_behind_the_one_it_names()
        => Assert.Equal(["B", "A"], Sorted(("A", ["B"]), ("B", [])));

    /// <summary>Name order decides nothing once a mod has said what it follows.</summary>
    [Fact]
    public void Naming_a_mod_that_sorts_later_still_puts_this_one_behind_it()
        => Assert.Equal(["C", "A", "B"], Sorted(("A", ["C"]), ("B", ["A"]), ("C", [])));

    [Fact]
    public void A_mod_may_follow_several()
    {
        var order = Sorted(("A", ["B", "C"]), ("B", []), ("C", []));

        Assert.Equal("A", order[2]);
        Assert.Equal(["B", "C"], order.Take(2));
    }

    /// <summary>
    /// A mod naming one that is not loaded is the ordinary case of an optional dependency, not an error: there
    /// is nothing to run behind, so it runs where it would have anyway.
    /// </summary>
    [Fact]
    public void Naming_a_mod_that_is_not_loaded_is_not_an_error()
        => Assert.Equal(["A", "B"], Sorted(("A", ["Absent"]), ("B", [])));

    /// <summary>
    /// A cycle has no order to run in, and guessing one would give a different answer per load. It fails at
    /// startup, naming the mods, because that is the only place anybody can fix it.
    /// </summary>
    [Fact]
    public void A_cycle_fails_and_names_the_mods_in_it()
    {
        var thrown = Assert.Throws<InvalidOperationException>(
            () => Sorted(("A", ["B"]), ("B", ["A"]), ("C", [])));

        Assert.Contains("A", thrown.Message);
        Assert.Contains("B", thrown.Message);
        Assert.DoesNotContain("C", thrown.Message);
    }

    [Fact]
    public void A_mod_that_follows_itself_is_a_cycle()
        => Assert.Throws<InvalidOperationException>(() => Sorted(("A", ["A"])));

    [Fact]
    public void One_mod_needs_no_sorting()
        => Assert.Equal(["A"], Sorted(("A", [])));

    [Fact]
    public void Nothing_to_sort_is_not_an_error()
        => Assert.Empty(Sorted());
}
