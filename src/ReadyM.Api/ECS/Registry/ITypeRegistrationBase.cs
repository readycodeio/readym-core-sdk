namespace ReadyM.Api.ECS.Registry;

internal interface ITypeRegistrationBase<in TRegistry>
{
    void Register(TRegistry registry);
}
