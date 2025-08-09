using System;
using System.Collections.Generic;
using System.Threading;
using LiteNetLib.Utils;

namespace ReadyM.Api.Helpers;

public abstract class PendingActionSchedulerBase
{
    protected const int MaxGroupCount = 256;
    protected const int MaxPendingItemCount = 2048;
    protected const int MaxPendingGroupItemCount = 512;
    
    protected Thread? _thread;
    
    public void EnsureThread()
    {
        if (_thread != null && _thread != Thread.CurrentThread)
            throw new InvalidOperationException("This call should be made from the thread that created the scheduler.");
    }
    
    public NetDataReader MakeSafe(NetDataReader reader)
    {
        if (_thread == null || _thread == Thread.CurrentThread)
            return reader;
        
        var bytesToCopy = reader.GetRemainingBytes();
        var readerCopy = new NetDataReader(bytesToCopy, 0, bytesToCopy.Length);
        return readerCopy;
    }
    
    public NetDataWriter MakeSafe(NetDataWriter writer)
    {
        if (_thread == null || _thread == Thread.CurrentThread)
            return writer;
        
        var writerCopy = new NetDataWriter(true, writer.Length);
        writerCopy.PutArray(writer.Data, writer.Length);
        return writerCopy;
    }
    
    public List<T> MakeSafe<T>(List<T> lst)
    {
        if (_thread == null || _thread == Thread.CurrentThread)
            return lst;

        return new List<T>(lst);
    }
    
    public ReadOnlyList<T> MakeSafe<T>(ReadOnlyList<T> lst)
    {
        if (_thread == null || _thread == Thread.CurrentThread)
            return lst;

        return new([..lst]);
    }

    public abstract bool Update();
}