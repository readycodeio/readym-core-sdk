using ReadyM.SDK.Archetypes.Core;

namespace ReadyM.SDK.Server.Seeding;

/// <summary>
/// Fills a new area. Declare it on a <c>[Service]</c>; the server calls it once, when the area is created rather
/// than loaded from the save.
/// </summary>
/// <remarks>
/// Game thread, inside the area's creation, before its created event. The server has not registered the area yet, so
/// create into it through its scope, <c>IEntities.Create&lt;T&gt;(Scope.Of(area))</c>. A mod has at most one area seeder.
/// </remarks>
public interface IAreaSeeder
{
    void Seed(Area area);
}
