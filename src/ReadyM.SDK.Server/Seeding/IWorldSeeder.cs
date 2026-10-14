using ReadyM.SDK.Archetypes.Core;

namespace ReadyM.SDK.Server.Seeding;

/// <summary>
/// Fills a new World. Declare it on a <c>[Service]</c>; the server calls it once, when the World is created rather
/// than loaded from the save.
/// </summary>
/// <remarks>
/// Game thread, inside the World's creation, before its created event. Entities without a scope belong to the World,
/// so create them with <c>IEntities.Create&lt;T&gt;()</c>. A mod has at most one world seeder. It runs as the
/// server starts, before any player has connected.
/// </remarks>
public interface IWorldSeeder
{
    void Seed(World world);
}
