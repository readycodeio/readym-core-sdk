using System;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Interop;

/// <summary>
/// The one rule for which holder of an indexed key a lookup answers with, kept by the host, the mod and the test relay
/// alike: the first holder the filter sees. A unique lookup is refused when another seen holder is in another
/// dimension. Holders are offered one at a time; a dimension is asked for only when the rule needs it.
/// </summary>
internal struct HolderChoice<T>
{
    private readonly DimensionFilter _filter;
    private readonly bool _unique;
    private DimensionId? _holderDimension;

    public HolderChoice(DimensionFilter filter, bool unique)
    {
        _filter = filter;
        _unique = unique;
    }

    public T Holder { get; private set; } = default!;

    public bool IsUnique => _unique;

    public bool Found { get; private set; }

    /// <summary>A unique lookup saw holders in two dimensions; <see cref="Holder"/> is not the answer.</summary>
    public bool Ambiguous { get; private set; }

    /// <summary>Offers one holder. True once the choice is settled, so the caller can stop offering.</summary>
    public bool Offer(T holder, Func<T, DimensionId> dimensionOf)
    {
        DimensionId? dimension = null;

        if (!_filter.IsEveryDimension)
        {
            dimension = dimensionOf(holder);
            if (!_filter.Sees(dimension.Value))
                return false;
        }

        if (!Found)
        {
            Holder = holder;
            Found = true;
            _holderDimension = dimension;
            return !_unique;
        }

        // Only a unique lookup gets here: a second holder must be in the first one's dimension
        _holderDimension ??= dimensionOf(Holder);
        if ((dimension ?? dimensionOf(holder)) == _holderDimension.Value)
            return false;

        Ambiguous = true;
        return true;
    }
}
