namespace ReadyM.Api.Mapping.Api;

public delegate TValue DataLoaderDelegate<TComponent, out TValue>(ref TComponent component);

public delegate TValue DataLoaderDelegate<TComponent, in TContext, out TValue>(ref TComponent component, TContext ctx);