using ReadyM.SDK.Archetypes.Core;

namespace ReadyM.SDK.Server.Seeding;

/// <summary>
/// Fills a new cell. Declare it on a <c>[Service]</c>; the server calls it once, when the cell is created rather
/// than loaded from the save.
/// </summary>
/// <remarks>
/// Game thread, inside the cell's creation, before its created event. The server has not registered the cell yet, so
/// create into it through its scope, <c>IEntities.Create&lt;T&gt;(Scope.Of(cell))</c>. A mod has at most one cell seeder.
/// No player is in the cell yet: what is created is the server's until the first player enters.
/// </remarks>
public interface ICellSeeder
{
    void Seed(Cell cell);
}
