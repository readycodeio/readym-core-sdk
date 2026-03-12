namespace ReadyM.Api.ECS.Jobs;

internal interface IJob
{
    void Execute();
}

internal interface IJob<in T>
{
    void Execute(T arg);
}

internal interface IJob<in T0, in T1>
{
    void Execute(T0 arg0, T1 arg1);
}

internal interface IJob<in T0, in T1, in T2>
{
    void Execute(T0 arg0, T1 arg1, T2 arg2);
}

internal interface IJob<in T0, in T1, in T2, in T3>
{
    void Execute(T0 arg0, T1 arg1, T2 arg2, T3 arg3);
}