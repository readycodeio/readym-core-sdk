namespace ReadyM.Api.ECS.Registry;

/// <summary>
/// Contributes types to <see cref="AllTypeRegistry"/>. One implementation per source: the default registration for
/// components every game shares, one per game for that game's own, one for what mods declare, and one per
/// assembly for its game events.
/// <para>
/// Register a type in exactly one of them. Registration does not deduplicate, so a type registered twice gets
/// two component ids out of a byte-wide space and a second change component. `AreaScopeComponent` belongs to
/// the default registration, not to Wukong's and Oblivion's as well.
/// </para>
/// </summary>
internal interface IAllTypeRegistration : ITypeRegistrationBase<IAllTypeRegistry>
{
    // empty
}
