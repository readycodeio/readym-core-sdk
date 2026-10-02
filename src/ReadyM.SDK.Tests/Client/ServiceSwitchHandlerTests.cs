using ReadyM.SDK.Services;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// A disabled service hears nothing: its create and delete handlers are switched off with its
/// update, so it does not act on a world it is not watching.
public class ServiceSwitchHandlerTests : ClientSdkTest
{
    [Fact]
    public void A_disabled_service_is_not_told_a_shape_it_watches_appeared()
    {
        Services.Disable<Turnstile>();

        Entities.Create<Ticketed>();

        Assert.Equal(0, Container.Resolve<Turnstile>().Inside);
    }

    [Fact]
    public void A_disabled_service_is_not_told_a_shape_it_watches_went()
    {
        var ticketed = Entities.Create<Ticketed>();

        Services.Disable<Turnstile>();
        Entities.Delete(ticketed);

        Assert.Equal(1, Container.Resolve<Turnstile>().Inside);
    }

    [Fact]
    public void Enabling_it_again_puts_its_handlers_back()
    {
        var services = Services;

        services.Disable<Turnstile>();
        Entities.Create<Ticketed>();
        services.Enable<Turnstile>();
        Entities.Create<Ticketed>();

        Assert.Equal(1, Container.Resolve<Turnstile>().Inside);
    }

    /// Switching one off says nothing about the rest, including a shape's own handlers.
    [Fact]
    public void Everything_else_still_hears_about_it()
    {
        Services.Disable<Undertaking>();

        Entities.Delete(Entities.Create<Crateload>());

        Assert.Equal(["shape"], Container.Resolve<Ledger>().Order);
    }

    private IServices Services => Container.Resolve<IServices>();
}
