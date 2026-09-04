using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Interop;
using Yooni.Native.Container;

namespace ReadyM.Relay.Server.Sdk.Ecs.Components;

/// <summary>
/// Translates a component's .NET type into the numeric id the server uses for it.
/// <para>
/// A mod names a component by type; the server names it by an id it assigns while building the ECS schema,
/// which happens after every mod has declared its components. So the mod cannot work the id out for itself and
/// has to ask, by full type name. This is what does the asking, and caches each answer.
/// </para>
/// <para>
/// It exists only from mod initialization onwards, because the function that answers is part of the second
/// initialization phase. That is the point of it being separate from <see cref="ComponentRegistry"/>, which
/// exists in the first phase: declaring a component and knowing its id are two different times, and keeping
/// them in one object meant an object that was only half usable for part of its life.
/// </para>
/// </summary>
internal sealed class ModComponentIds
{
    private readonly GetComponentIdByNameDelegate _getComponentIdByName;
    private readonly Dictionary<Type, int> _resolved = new();

    /// <param name="getComponentIdByName">
    /// The server's answering function, as an interop pointer. Supplied by the host at mod initialization.
    /// </param>
    internal ModComponentIds(IntPtr getComponentIdByName)
    {
        if (getComponentIdByName == IntPtr.Zero)
            throw new ArgumentException("The host did not supply a component id resolver.", nameof(getComponentIdByName));

        _getComponentIdByName = Marshal.GetDelegateForFunctionPointer<GetComponentIdByNameDelegate>(getComponentIdByName);
    }

    /// <summary>
    /// The id the server assigned to a component, asked for once and cached.
    /// <para>
    /// This is the only path, including for components this mod registered itself. Registering merely tells
    /// the server the component exists; the id is decided later, when the server registers everything into
    /// the schema and reads the result back.
    /// </para>
    /// </summary>
    internal int Resolve<T>() where T : struct
    {
        if (_resolved.TryGetValue(typeof(T), out var cached))
            return cached;

        var id = _getComponentIdByName(new NativeString256(typeof(T).FullName, false));
        if (id < 0)
        {
            throw new InvalidOperationException(
                $"The server does not know component {typeof(T).FullName}. Either it was never registered, or "
                + "this ran before the server finished building its component table.");
        }

        _resolved.Add(typeof(T), id);
        return id;
    }
}
