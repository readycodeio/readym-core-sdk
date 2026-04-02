using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.ECS.Systems;

internal class QueryCacheHelper<TContext, TKey, TQuery>(Func<TContext, TKey> keyFunc, Func<TContext, TQuery> queryFactory)
    where TQuery : ArchetypeQuery
{
    private TQuery? _nullQuery;
    private readonly Dictionary<TKey, TQuery> _queryCache = new();

    public TQuery GetQuery(TContext context)
    {
        var key = keyFunc(context);
        
        TQuery query;
        if (key == null)
        {
            if (_nullQuery == null)
            {
                _nullQuery = queryFactory.Invoke(context);
            }
            query = _nullQuery;
        }
        else
        {
            if (!_queryCache.TryGetValue(key, out query))
            {
                query = queryFactory.Invoke(context);
                _queryCache.Add(key, query);
            }
        }

        return query;
    }
}