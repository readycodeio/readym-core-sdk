using Xunit;

// The SDK keeps a few things a game sets once and everything reads: what a create handler resolves
// its services from, what a sync point maps through, whether mod systems are running. A test class
// sets those for itself and disposes what it built, so two running at once can leave a handler
// resolving from a container that has already gone. The suite is a fifth of a second either way.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
