using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Tests.Client.Fixtures;

/// A role a host looks services up by, the way the server finds mod seeders.
public interface IGreeter
{
    string Greet();
}

/// Declared after its sibling in the file but named before it, so the order is the name's.
[Service]
public sealed partial class ZuluGreeter : IGreeter
{
    public string Greet() => "zulu";
}

[Service]
public sealed partial class AlphaGreeter : IGreeter
{
    public string Greet() => "alpha";
}
