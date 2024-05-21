using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Xunit;
// ReSharper disable UnusedMember.Local
// ReSharper disable ClassNeverInstantiated.Local

namespace ConsoleAppFramework.Integration.Test;

public class FilterTest
{
    [Fact]
    public void ApplyAttributeFilterTest()
    {
        using var console = new CaptureConsoleOutput();
        var args = new[] { "test-argument-name" };
        Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<TestConsoleApp>(args);
        console.Output.Should().Contain("[in filter] before");
        console.Output.Should().Contain(args[0]);
        console.Output.Should().Contain("[in filter] after");
    }

    /// <inheritdoc />
    private class TestConsoleApp : ConsoleAppBase
    {
        [RootCommand]
        [ConsoleAppFilter(typeof(TestFilter))]
        public void RootCommand([Option(index: 0)] string someArgument) => Console.WriteLine(someArgument);
    }

    /// <inheritdoc />
    private class TestFilter : ConsoleAppFilter
    {
        /// <inheritdoc />
        public override async ValueTask Invoke(ConsoleAppContext context, Func<ConsoleAppContext, ValueTask> next)
        {
            Console.WriteLine("[in filter] before");
            await next(context);
            Console.WriteLine("[in filter] after");
        }
    }
}

