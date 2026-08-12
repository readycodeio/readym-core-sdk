namespace ReadyM.Api.Mapping.Data;

internal delegate TValue DataLoaderDelegate<TComponent, out TValue>(ref TComponent component);

internal delegate TValue DataLoaderDelegate<TComponent, in TContext, out TValue>(ref TComponent component, TContext ctx);