namespace ReadyM.Api.Generators.Derive;

internal interface IDeriveSupportImpl<in TItem, in TContext> : IDeriveSupportImplBase<TItem>
{
    public void Visit(TItem item, TContext context);
}