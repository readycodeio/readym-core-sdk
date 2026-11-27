using System.Runtime.InteropServices;
using ReadyM.Api.Idents;
using ReadyM.Relay.Server.Sdk.Interop;
using ReadyM.SDK.Server.Dimensions;

namespace ReadyM.SDK.Tests.Server;

/// <summary>The host answers a refused dimension call with a code; the SDK throws what the host's service would.</summary>
public class ServerDimensionApiTests
{
    // Rooted for the test's life: the api holds only their function pointers.
    private readonly List<Delegate> _rooted = [];

    private unsafe DimensionApi ApiAnswering(DimensionCallResult result)
    {
        CreateDimensionDelegate create = (_, created) =>
        {
            *created = new DimensionId(7);
            return result;
        };
        DestroyDimensionDelegate destroy = _ => result;
        SetPlayerDimensionDelegate setPlayer = (_, _) => result;
        DimensionExistsDelegate exists = _ => 1;
        TryFindDimensionDelegate tryFind = (_, found) =>
        {
            *found = new DimensionId(7);
            return 1;
        };
        PlayerDimensionDelegate ofPlayer = _ => new DimensionId(7);
        CurrentDimensionDelegate currentDimension = _ => DimensionId.Main;
        EntityDimensionDelegate ofEntity = _ => DimensionId.Shared;
        IsEntityOriginDimensionDelegate isOrigin = (_, dimension) => dimension == new DimensionId(7) ? (byte)1 : (byte)0;
        ResolveAreaDelegate resolveArea = (from, _) => from == new DimensionId(7) ? DimensionId.Main : from;

        _rooted.AddRange([create, destroy, setPlayer, exists, tryFind, ofPlayer, currentDimension, ofEntity, isOrigin, resolveArea]);

        return new DimensionApi(new DimensionApiPointers
        {
            Create = Marshal.GetFunctionPointerForDelegate(create),
            Destroy = Marshal.GetFunctionPointerForDelegate(destroy),
            SetPlayerDimension = Marshal.GetFunctionPointerForDelegate(setPlayer),
            Exists = Marshal.GetFunctionPointerForDelegate(exists),
            TryFind = Marshal.GetFunctionPointerForDelegate(tryFind),
            PlayerDimension = Marshal.GetFunctionPointerForDelegate(ofPlayer),
            CurrentDimension = Marshal.GetFunctionPointerForDelegate(currentDimension),
            EntityDimension = Marshal.GetFunctionPointerForDelegate(ofEntity),
            IsEntityOriginDimension = Marshal.GetFunctionPointerForDelegate(isOrigin),
            ResolveArea = Marshal.GetFunctionPointerForDelegate(resolveArea),
        });
    }

    // The result as its number: the enum is internal, and a public test cannot take it.
    public static TheoryData<byte, Type> Refusals => new()
    {
        { (byte)DimensionCallResult.NotLive, typeof(KeyNotFoundException) },
        { (byte)DimensionCallResult.NotConnected, typeof(KeyNotFoundException) },
        { (byte)DimensionCallResult.NameRequired, typeof(ArgumentException) },
        { (byte)DimensionCallResult.NameTaken, typeof(InvalidOperationException) },
        { (byte)DimensionCallResult.ParentNotLive, typeof(InvalidOperationException) },
        { (byte)DimensionCallResult.HasChildren, typeof(InvalidOperationException) },
        { (byte)DimensionCallResult.IsMain, typeof(InvalidOperationException) },
        { (byte)DimensionCallResult.Destroying, typeof(InvalidOperationException) },
    };

    [Theory]
    [MemberData(nameof(Refusals))]
    public void Each_refusal_is_the_service_exceptions_type(byte result, Type exception)
    {
        var api = ApiAnswering((DimensionCallResult)result);

        Assert.IsType(exception, Record.Exception(() => api.Create(new DimensionOptions())));
        Assert.IsType(exception, Record.Exception(() => api.Destroy(new DimensionId(7))));
        Assert.IsType(exception, Record.Exception(() => api.SetPlayerDimension(new PlayerId(1), new DimensionId(7))));
    }

    [Fact]
    public void A_call_that_is_done_answers_what_the_host_wrote()
    {
        var api = ApiAnswering(DimensionCallResult.Done);

        Assert.Equal(new DimensionId(7), api.Create(new DimensionOptions { Name = "lobby", Areas = new HashSet<AreaId> { new("a") } }));
        api.SetPlayerDimension(new PlayerId(1), new DimensionId(7));
        Assert.True(api.TryFind("lobby", out var found));
        Assert.Equal(new DimensionId(7), found);
        api.Destroy(new DimensionId(7));
    }

    [Fact]
    public void Whether_a_dimension_is_an_entitys_origin_is_the_hosts_answer()
    {
        var api = ApiAnswering(DimensionCallResult.Done);

        Assert.True(api.IsEntityOriginDimension(default, new DimensionId(7)));
        Assert.False(api.IsEntityOriginDimension(default, DimensionId.Main));
    }

    [Fact]
    public void An_area_resolves_to_the_dimension_the_host_answers()
    {
        var api = ApiAnswering(DimensionCallResult.Done);
        var area = new AreaId("Tamriel");

        Assert.Equal(new FullAreaId(DimensionId.Main, area), api.ResolveArea(new DimensionId(7), area));
        Assert.Equal(new FullAreaId(new DimensionId(3), area), api.ResolveArea(new DimensionId(3), area));
    }
}
