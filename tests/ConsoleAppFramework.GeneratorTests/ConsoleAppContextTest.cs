using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace ConsoleAppFramework.GeneratorTests;

public class ConsoleAppContextTest(ITestOutputHelper output)
{
    VerifyHelper verifier = new VerifyHelper(output, "CAF");

    [Fact]
    public void ForLambda()
    {
        verifier.Execute("""
ConsoleApp.Run(args, (ConsoleAppContext ctx) => { Console.Write(ctx.Arguments.Length); });
""", args: "", expected: "0");
    }

    [Fact]
    public void ForMethod()
    {
        verifier.Execute("""
var builder = ConsoleApp.Create();

builder.UseFilter<StateFilter>();

builder.Add("", Hello);

builder.Run(args);

void Hello(ConsoleAppContext ctx)
{
    Console.Write(ctx.State);
}

internal class StateFilter(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context,CancellationToken cancellationToken)
    {
        Console.Write(1);
        return Next.InvokeAsync(context with { State = 2 }, cancellationToken);
    }
}
""", args: "", expected: "12");
    }
}
