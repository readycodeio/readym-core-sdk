namespace ReadyM.SDK.Attributes;

/// How a replicated shape's changes reach the other side.
public enum Delivery
{
    /// Guaranteed to arrive, in the order it was sent.
    Reliable,

    /// Sent once and never resent. Useful for a value written often enough that losing one is corrected by
    /// the next write, such as a position.
    Unreliable
}
