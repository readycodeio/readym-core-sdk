using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// A [CreateHandler(typeof(Shape))] in a service watches a shape the service did not declare, and
/// the shape it watches may live in another assembly.
public class ServiceCreateHandlerTests : ClientSdkTest
{
    [Fact]
    public void A_service_is_told_when_a_shape_it_watches_appears()
    {
        Entities.Create<Parcel>();
        Entities.Create<Parcel>();

        Assert.Equal(2, Container.Resolve<Bookkeeping>().Seen.Count);
    }

    /// The shape's own handler runs first, over the whole entity, so a watcher reads what it left.
    [Fact]
    public void A_service_sees_what_the_shape_set_for_itself()
    {
        Entities.Create<Parcel>();

        Assert.Equal([7], Container.Resolve<Bookkeeping>().Seen);
    }

    /// <summary>
    /// A module initializer already ran this assembly's registrations, and a loader calls them again
    /// for every assembly it loads. Watching a shape appends, so the second call has to do nothing.
    /// </summary>
    [Fact]
    public void A_loader_saying_it_again_does_not_run_the_handler_twice()
    {
        GeneratedShapes.Apply(typeof(Bookkeeping).Assembly);
        GeneratedShapes.Apply(typeof(Bookkeeping).Assembly);

        Entities.Create<Parcel>();

        Assert.Single(Container.Resolve<Bookkeeping>().Seen);
    }
}
