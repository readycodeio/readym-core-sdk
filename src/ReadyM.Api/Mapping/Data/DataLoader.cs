namespace ReadyM.Api.Mapping.Data;

internal delegate void DataLoader<TComponent, in TContext>(ref TComponent component, TContext ctx);