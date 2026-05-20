namespace ReadyM.Api.Generators;

internal class DeriveMapSettings(
    bool mapFields,
    bool mapProperties,
    bool mapPrivate,
    bool mapPublic,
    bool mapInternal)
{
    public bool MapFields { get; } = mapFields;
    public bool MapProperties { get; } = mapProperties;
    public bool MapPrivate { get; } = mapPrivate;
    public bool MapPublic { get; } = mapPublic;
    public bool MapInternal { get; } = mapInternal;
}