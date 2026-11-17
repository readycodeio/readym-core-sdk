using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using System;

namespace ReadyM.Api.Multiplayer.Interop;

/// <summary>The dimension of each row of an archetype, as the side that holds the rows can read it.</summary>
internal interface IRowDimensions
{
    DimensionId At(int row);
}

/// <summary>
/// Hands a chunk to a callback as the runs of rows a dimension filter sees, each slot moved to its run's first row.
/// The host and the test relay cut chunks the same way through this.
/// </summary>
internal static class ChunkRuns
{
    public static unsafe void Hand<TRows>(
        in TRows rows,
        DimensionFilter filter,
        RawEntity* entities,
        ChunkComponent* comps,
        ChunkComponent* run,
        int n,
        int count,
        ChunkCallback callback)
        where TRows : struct, IRowDimensions
    {
        var row = 0;

        while (row < count)
        {
            if (!filter.Sees(rows.At(row)))
            {
                row++;
                continue;
            }

            var start = row;

            while (row < count && filter.Sees(rows.At(row)))
                row++;

            for (var i = 0; i < n; i++)
                run[i] = comps[i].From(start);

            callback((IntPtr)(entities + start), run, n, row - start);
        }
    }
}
