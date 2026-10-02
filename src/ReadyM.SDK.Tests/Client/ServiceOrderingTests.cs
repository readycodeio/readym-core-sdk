using ReadyM.SDK.Services;

namespace ReadyM.SDK.Tests.Client;

/// <summary>
/// The order services update in: Before and After are hard, the priority decides the rest.
/// </summary>
/// <remarks>
/// Worked on plain names rather than on real services, because what is being pinned is the rule,
/// and a fixture per case would say less than the constraint it stands for.
/// </remarks>
public class ServiceOrderingTests
{
    private sealed record Node(Type Type, ServiceOrder Order);

    private sealed class A;
    private sealed class B;
    private sealed class C;
    private sealed class D;

    private static Node Of<T>(int priority = 100, Type[]? before = null, Type[]? after = null)
        => new(typeof(T), new ServiceOrder(priority, before, after));

    private static string Sort(params Node[] nodes)
        => string.Join(
            " ",
            ServiceOrdering.Sort(nodes, node => node.Type, node => node.Order)
                .Select(node => node.Type.Name));

    // -- priority ----------------------------------------------------------------------------------

    /// Lower runs earlier, which is the whole of what a priority says.
    [Fact]
    public void A_lower_priority_runs_first()
        => Assert.Equal("B A", Sort(Of<A>(100), Of<B>(0)));

    /// Nothing distinguishes them, so the name does, and the same install always ticks the same way.
    [Fact]
    public void Equal_priorities_fall_back_to_the_name()
        => Assert.Equal("A B", Sort(Of<B>(), Of<A>()));

    // -- constraints -------------------------------------------------------------------------------

    [Fact]
    public void Before_puts_one_ahead_of_another()
        => Assert.Equal("A B", Sort(Of<A>(before: [typeof(B)]), Of<B>()));

    [Fact]
    public void After_puts_one_behind_another()
        => Assert.Equal("B A", Sort(Of<A>(after: [typeof(B)]), Of<B>()));

    /// The constraint wins: A asks to be first and is still held until B has run.
    [Fact]
    public void A_constraint_beats_a_priority()
        => Assert.Equal("B A", Sort(Of<A>(0, after: [typeof(B)]), Of<B>(100)));

    /// Among what the constraints leave free, the priority still decides.
    [Fact]
    public void The_priority_decides_what_the_constraints_leave_open()
        => Assert.Equal("C B A", Sort(Of<A>(100, after: [typeof(B)]), Of<B>(50), Of<C>(0)));

    // -- what cannot be ordered --------------------------------------------------------------------

    /// A mod naming another mod's service is ordinary, and that mod may not be installed.
    [Fact]
    public void A_target_that_is_not_here_is_dropped()
    {
        var dropped = new List<(Type Named, Type By)>();

        var sorted = ServiceOrdering.Sort(
            [Of<A>(after: [typeof(D)]), Of<B>()],
            node => node.Type,
            node => node.Order,
            (named, by) => dropped.Add((named, by)));

        Assert.Equal("A B", string.Join(" ", sorted.Select(node => node.Type.Name)));
        Assert.Equal((typeof(D), typeof(A)), Assert.Single(dropped));
    }

    [Fact]
    public void A_cycle_is_refused()
    {
        var refused = Assert.Throws<ServiceOrderException>(()
            => Sort(Of<A>(before: [typeof(B)]), Of<B>(before: [typeof(A)])));

        Assert.Contains("A", refused.Cycle);
        Assert.Contains("B", refused.Cycle);
    }

    /// Named rather than counted, so the message says which constraints to look at.
    [Fact]
    public void A_longer_cycle_is_named()
    {
        var refused = Assert.Throws<ServiceOrderException>(() => Sort(
            Of<A>(before: [typeof(B)]),
            Of<B>(before: [typeof(C)]),
            Of<C>(before: [typeof(A)])));

        Assert.Contains("->", refused.Cycle);
        Assert.Contains("C", refused.Cycle);
    }

    /// One service out of a loop is enough to keep the rest orderable.
    [Fact]
    public void Everything_outside_a_loop_still_orders()
        => Assert.Equal("A B C", Sort(Of<A>(before: [typeof(B)]), Of<B>(before: [typeof(C)]), Of<C>()));
}
