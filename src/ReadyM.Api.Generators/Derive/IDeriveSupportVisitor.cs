namespace ReadyM.Api.Generators.Derive;

internal interface IDeriveSupportVisitor<in TItem, in TContext>
{
    public void Visit(TItem item, TContext context);
}