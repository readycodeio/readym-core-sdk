using System;

namespace ReadyM.Api.Multiplayer.Interning;

/// <summary>The index generated code writes and reads interned strings through on this thread.</summary>
internal static class InternedStrings
{
    [field: ThreadStatic]
    public static IInternedStringIndex? Current { get; private set; }

    public static Scope Enter(IInternedStringIndex? index)
    {
        var previous = Current;
        Current = index;
        return new Scope(previous);
    }

    public struct Scope : IDisposable
    {
        private readonly IInternedStringIndex? _previous;
        private bool _disposed;

        internal Scope(IInternedStringIndex? previous)
        {
            _previous = previous;
            _disposed = false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Current = _previous;
        }
    }
}
