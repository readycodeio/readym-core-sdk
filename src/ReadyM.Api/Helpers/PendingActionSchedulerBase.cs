using System;
using System.Collections.Generic;
using System.Threading;
using LiteNetLib.Utils;

namespace ReadyM.Api.Helpers;

public abstract class PendingActionSchedulerBase
{
    protected Thread? _thread;

    public NetDataReader MakeSafe(NetDataReader reader)
    {
        if (_thread == null)
            throw new InvalidOperationException("Scheduler thread is not set. Ensure that the ECS update loop is running on the correct thread.");

        if (_thread == Thread.CurrentThread)
            return reader;
        
        var readerCopy = new NetDataReader(reader.RawData, reader.Position, reader.AvailableBytes);
        return readerCopy;
    }
    
    public NetDataWriter MakeSafe(NetDataWriter writer)
    {
        if (_thread == null)
            throw new InvalidOperationException("Scheduler thread is not set. Ensure that the ECS update loop is running on the correct thread.");

        if (_thread == Thread.CurrentThread)
            return writer;
        
        var writerCopy = new NetDataWriter(true, writer.Length);
        writerCopy.PutArray(writer.Data, writer.Length);
        return writerCopy;
    }
    
    public List<T> MakeSafe<T>(List<T> lst)
    {
        if (_thread == null)
            throw new InvalidOperationException("Scheduler thread is not set. Ensure that the ECS update loop is running on the correct thread.");

        if (_thread == Thread.CurrentThread)
            return lst;

        return new List<T>(lst);
    }
    
    public ReadOnlyList<T> MakeSafe<T>(ReadOnlyList<T> lst)
    {
        if (_thread == null)
            throw new InvalidOperationException("Scheduler thread is not set. Ensure that the ECS update loop is running on the correct thread.");

        if (_thread == Thread.CurrentThread)
            return lst;

        return new([..lst]);
    }
}