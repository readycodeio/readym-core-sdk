namespace ReadyM.Api.Generators.Derive;

internal interface IDeriveSupportImplBase<in TItem>
{
    public abstract bool Supports(TItem type);
}