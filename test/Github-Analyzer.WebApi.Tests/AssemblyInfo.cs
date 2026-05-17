using Xunit;

// Defines a named collection that runs its members sequentially (no parallelism).
// Apply [Collection("Sequential")] to any test class that uses timing-sensitive
// BackgroundService mechanics or shared in-memory resources.
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SequentialCollection { }
