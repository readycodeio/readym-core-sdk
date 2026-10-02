using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Client.Mapping;

public interface IShapeMappingRegistry
{
    /// Everything this shape maps to and from one kind of game object.
    IShapeMappingScope<TShape, TContext> For<TShape, TContext>()
        where TShape : struct, IArchetypeQueryable;
}