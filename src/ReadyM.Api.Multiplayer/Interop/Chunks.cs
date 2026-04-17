using System;
using System.Runtime.InteropServices;

namespace ReadyM.Api.Multiplayer.Interop;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Chunks1
{
    public IntPtr Chunk1;
    public int Length1;
    
    public unsafe Span<T> AsSpan<T>()
    {
        return new Span<T>((void*)Chunk1, Length1);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Chunks2
{
    public IntPtr Chunk1;
    public int Length1;

    public IntPtr Chunk2;
    public int Length2;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Chunks3
{
    public IntPtr Chunk1;
    public int Length1;

    public IntPtr Chunk2;
    public int Length2;

    public IntPtr Chunk3;
    public int Length3;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Chunks4
{
    public IntPtr Chunk1;
    public int Length1;

    public IntPtr Chunk2;
    public int Length2;

    public IntPtr Chunk3;
    public int Length3;

    public IntPtr Chunk4;
    public int Length4;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Chunks5
{
    public IntPtr Chunk1;
    public int Length1;

    public IntPtr Chunk2;
    public int Length2;

    public IntPtr Chunk3;
    public int Length3;

    public IntPtr Chunk4;
    public int Length4;

    public IntPtr Chunk5;
    public int Length5;
}