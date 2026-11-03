using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using Xunit;

namespace ReadyM.Api.Tests;

/// <summary>An archetype's id is one byte, so a world holds 256 of them.</summary>
public sealed class ArchetypeIdCapacityTests
{
    private struct PlainComponent : IComponent
    {
        public int Value;
    }

    private sealed class Registration(Action<IArchetypeRegistry> configure) : IArchetypeRegistration
    {
        public void Register(IArchetypeRegistry registry) => configure(registry);
    }

    /// <summary>The counter used to wrap round to 0, handing a second archetype the first one's id.</summary>
    [Fact]
    public void The_archetype_after_the_last_id_is_refused_rather_than_given_a_used_one()
    {
        var ids = new List<ArchetypeId>();
        InvalidOperationException? refused = null;

        _ = new Store(new EntityStore(), NullLogger.Instance, [
            new Registration(registry => refused = Assert.Throws<InvalidOperationException>(() =>
            {
                for (var i = 0; i < 300; i++)
                    ids.Add(registry.RegisterArchetype($"Archetype{i}", new ArchetypeBuilder().Add<PlainComponent>()));
            })),
        ]);

        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.Contains("256", refused!.Message);
    }
}
