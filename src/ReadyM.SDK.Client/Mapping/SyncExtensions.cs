using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Access;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Client.Mapping;

/// Extensions for moving one value between the ECS and the game at a sync point, usually a patch or system.
public static class SyncExtensions
{
    private static ShapeMappingRegistry? _mappings;

    internal static void Use(ShapeMappingRegistry mappings) => _mappings = mappings;

    private static ShapeMappingRegistry.Mapping<TValue, TContext>? Mapping<TOwner, TValue, TContext>(Value<TOwner, TValue> value)
        where TOwner : struct, IArchetypeQueryable
        => _mappings?.Find<TValue, TContext>(value.Access);

    extension<TShape>(TShape shape) where TShape : struct, IArchetypeQueryable
    {
        /// Whether this value should be applied from the ECS to the game.
        public bool CanPush<TOwner, TValue>(Value<TOwner, TValue> value) where TOwner : struct, IArchetypeQueryable
        {
            var handle = EntityHandle.Of(shape);

            return handle.ShouldApplyToGame(value.Access.Component) || value.Access.WasSetFromApi(handle);
        }

        /// Whether this collection should be applied from the ECS to the game.
        public bool CanPush<TAccess>(Collection<TShape, TAccess> collection)
#if NET10_0_OR_GREATER
            where TAccess : allows ref struct
#endif
        {
            var handle = EntityHandle.Of(shape);

            return handle.ShouldApplyToGame(collection.Value.Component) || collection.Value.WasSetFromApi(handle);
        }

        /// Whether this collection should be loaded from the game into the ECS.
        public bool CanPull<TAccess>(Collection<TShape, TAccess> collection)
#if NET10_0_OR_GREATER
            where TAccess : allows ref struct
#endif
        {
            var handle = EntityHandle.Of(shape);

            return !collection.Value.WasSetFromApi(handle)
                   && handle.Allows(collection.Value.Component, WriteKind.Mirror);
        }

        /// Shows the game what the collection holds. The handler is handed it to read, and the
        /// members that would change it answer no while it is being shown.
        public bool Push<TAccess, TContext>(Collection<TShape, TAccess> collection, TContext context)
#if NET10_0_OR_GREATER
            where TAccess : allows ref struct
#endif
        {
            if (!shape.CanPush(collection) || _mappings?.FindPush<TAccess, TContext>(collection.Value) is not { } push)
                return false;

            var handle = EntityHandle.Of(shape);

            push(collection.Open(in handle), context);
            collection.Value.ClearApiFlag(handle);

            return true;
        }

        /// Reads the collection back out of the game, through the members the component exposes.
        public bool Pull<TAccess, TContext>(Collection<TShape, TAccess> collection, TContext context)
#if NET10_0_OR_GREATER
            where TAccess : allows ref struct
#endif
        {
            if (!shape.CanPull(collection) || _mappings?.FindPull<TAccess, TContext>(collection.Value) is not { } pull)
                return false;

            var handle = EntityHandle.Of(shape);

            pull(collection.Open(in handle), context);

            return true;
        }

        /// Whether this shape should be applied from the ECS to the game.
        public bool CanPush(Shape<TShape> whole)
        {
            var handle = EntityHandle.Of(shape);

            return handle.ShouldApplyToGame(whole.Access.Component) || whole.Access.WasOverridden(handle);
        }

        /// Whether this value should be loaded from the game into the ECS.
        public bool CanPull<TOwner, TValue>(Value<TOwner, TValue> value) where TOwner : struct, IArchetypeQueryable
        {
            var handle = EntityHandle.Of(shape);

            return !value.Access.WasSetFromApi(handle)
                   && handle.Allows(value.Access.Component, WriteKind.Mirror);
        }

        /// Whether this shape should be loaded from the game into the ECS.
        public bool CanPull(Shape<TShape> whole)
        {
            var handle = EntityHandle.Of(shape);

            return !whole.Access.WasOverridden(handle) && handle.Allows(whole.Access.Component, WriteKind.Mirror);
        }

        /// Applies value the ECS holds to the game.
        /// <returns><c>false</c> when the application is not allowed, or nothing maps the field in this context.</returns>
        public bool Push<TOwner, TValue, TContext>(Value<TOwner, TValue> value,
            TContext context) where TOwner : struct, IArchetypeQueryable
        {
            // A mapping with no push is one the game owns: there is nothing to show it.
            if (!shape.CanPush(value) || Mapping<TOwner, TValue, TContext>(value)?.Push is not { } push)
                return false;

            var handle = EntityHandle.Of(shape);

            push(value.Access.Read(handle), context);
            value.Access.ClearApiFlag(handle);

            return true;
        }

        /// Reads the value back out of the game into the ECS.
        public bool Pull<TOwner, TValue, TContext>(Value<TOwner, TValue> value,
            TContext context) where TOwner : struct, IArchetypeQueryable
        {
            if (!shape.CanPull(value) || Mapping<TOwner, TValue, TContext>(value) is not { } mapping)
                return false;

            var handle = EntityHandle.Of(shape);
            var current = value.Access.Read(handle);

            mapping.Pull(ref current, context);
            value.Access.Set(handle, current);

            return true;
        }

        /// Applies the entire shape the ECS holds to the game.
        public bool Push<TContext>(Shape<TShape> whole, TContext context)
        {
            if (!shape.CanPush(whole) || _mappings?.FindShape<TShape, TContext>(whole.Access)?.Push is not { } push)
                return false;

            push(shape, context);
            whole.Access.Applied(EntityHandle.Of(shape));

            return true;
        }

        /// Reads the entire shape back out of the game, into the ECS.
        public bool Pull<TContext>(Shape<TShape> whole, TContext context)
        {
            if (!shape.CanPull(whole) || _mappings?.FindShape<TShape, TContext>(whole.Access) is not { } mapping)
                return false;

            var copy = shape;

            mapping.Pull(ref copy, context);

            return true;
        }
    }
}