using System.Runtime.InteropServices;

namespace ReadyM.Relay.Server.Sdk.Interop;

/// <exclude/>
[StructLayout(LayoutKind.Sequential)]
public struct EcsApiPointers
{
    /// <summary>Arity-agnostic query. Supersedes Query1..Query6.</summary>
    public required IntPtr Query;

    public required IntPtr CreateNetworkedEntity;
    public required IntPtr CreateNetworkedPlayerEntity;
    public required IntPtr CreateNetworkedAreaEntity;
    public required IntPtr CreateNetworkedCellEntity;
    public required IntPtr CreateLocalEntity;
    public required IntPtr DeleteNetworkedEntity;
    public required IntPtr DeleteEntityTree;
    public required IntPtr SetParent;
    public required IntPtr GetParent;
    public required IntPtr GetChildren;
    public required IntPtr GetComponentSlot;

    /// <summary>Writes a whole component, keeping the index the owning side holds for it current.</summary>
    public required IntPtr SetComponent;

    /// <summary>Finds the entity whose indexed component holds a value.</summary>
    public required IntPtr FindByIndex;

    /// <summary>The entities a scope holds that carry a component set.</summary>
    public required IntPtr QueryInScope;

    /// <summary>Creates an entity held by a scope.</summary>
    public required IntPtr CreateLocalEntityInScope;
    public required IntPtr CreateNetworkedEntityInScope;
    public required IntPtr IsEntityAlive;
    public required IntPtr TryGetAreaScopeEntity;
    public required IntPtr CreateAreaScopeEntity;
    public required IntPtr TryGetCellScopeEntity;
    public required IntPtr CreateCellScopeEntity;
    public required IntPtr TryGetEntityCell;

    /// <summary>The scope an entity sits in, one step up.</summary>
    public required IntPtr TryGetEntityScope;

    /// <summary>
    /// Resolves a component id from a full type name. Phase two, deliberately: the host cannot answer until
    /// it has built its component table, which happens after the schema is created. Mods only ask when they
    /// register an archetype or run a query, both of which are phase two or later.
    /// </summary>
    public required IntPtr GetComponentIdByName;

    /// <summary>Whether the caller is on the world's thread, which raising an event requires.</summary>
    public required IntPtr IsOnWorldThread;

    /// <summary>Makes an entity in a cell a resident. Zero, as the two below, on a host that keeps no residents.</summary>
    public required IntPtr MakeResident;

    /// <summary>Makes a resident standing in a cell that cell's content again.</summary>
    public required IntPtr EndResidency;

    /// <summary>Whether an entity is a resident.</summary>
    public required IntPtr IsResident;
}