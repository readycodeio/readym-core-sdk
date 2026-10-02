using Xunit;

// The SDK keeps its registries in statics, and a create handler resolves out of the one container
// CreateHandlerRegistry was last told about. Two classes running at once overwrite each other's.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
