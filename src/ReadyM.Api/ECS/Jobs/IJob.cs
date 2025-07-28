namespace ReadyM.Api.ECS.Jobs;

public interface IJob
{
    public void Execute();
}

public interface IJob<in T>
{
    public void Execute(T arg);
}

public interface IJob<in T0, in T1>
{
    public void Execute(T0 arg0, T1 arg1);
}

public interface IJob<in T0, in T1, in T2>
{
    public void Execute(T0 arg0, T1 arg1, T2 arg2);
}