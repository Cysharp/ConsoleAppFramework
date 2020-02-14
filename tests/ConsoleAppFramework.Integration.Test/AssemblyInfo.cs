using Xunit;

// NOTE: This test project contains integration tests that use `Console.Out` directly. Therefore, the tests must be run sequentially.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true)]