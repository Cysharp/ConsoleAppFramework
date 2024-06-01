global using Xunit;
global using Xunit.Abstractions;
global using FluentAssertions;

// CSharpGeneratorRunner.CompileAndExecute uses stdout hook(replace Console.Out)
// so can not work in parallel test
[assembly: CollectionBehavior(DisableTestParallelization = true)]