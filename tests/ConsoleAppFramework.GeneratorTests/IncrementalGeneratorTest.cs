using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppFramework.GeneratorTests;

public class IncrementalGeneratorTest
{
    [Fact]
    public void RunLambda()
    {
        var step1 = """
using ConsoleAppFramework;

ConsoleApp.Run(args, int () => 0);
""";

        var step2 = """
using ConsoleAppFramework;

ConsoleApp.Run(args, int () => 100); // body change

Console.WriteLine("foo"); // unrelated line
""";

        var step3 = """
using ConsoleAppFramework;

ConsoleApp.Run(args, int (int x, int y) => 99); // change signature

Console.WriteLine("foo"); // unrelated line
""";

        var reasons = CSharpGeneratorRunner.GetIncrementalGeneratorTrackedStepsReasons("ConsoleApp.Run", step1, step2, step3);

        reasons[0][0].Reasons.Should().Be("New");
        reasons[1][0].Reasons.Should().Be("Unchanged");
        reasons[2][0].Reasons.Should().Be("Modified");
    }

    [Fact]
    public void RunMethod()
    {
        var step1 = """
using ConsoleAppFramework;

var tako = new Tako();
ConsoleApp.Run(args, tako.DoHello);

public class Tako
{
    /// <summary>
    /// AAAAA
    /// </summary>
    public void DoHello()
    {
    }
}
""";

        var step2 = """
using ConsoleAppFramework;

var tako = new Tako();
ConsoleApp.Run(args, tako.DoHello);

Console.WriteLine("foo"); // unrelated line

public class Tako
{
    /// <summary>
    /// AAAAA
    /// </summary>
    public void DoHello()
    {
        Console.WriteLine("foo"); // body change
    }
}
""";

        var step3 = """
using ConsoleAppFramework;

var tako = new Tako();
ConsoleApp.Run(args, tako.DoHello);

public class Tako
{
    /// <summary>
    /// AAAAA
    /// </summary>
    public void DoHello(int x, int y) // signature change
    {
    }
}
""";

        var reasons = CSharpGeneratorRunner.GetIncrementalGeneratorTrackedStepsReasons("ConsoleApp.Run", step1, step2, step3);

        reasons[0][0].Reasons.Should().Be("New");
        reasons[1][0].Reasons.Should().Be("Unchanged");
        reasons[2][0].Reasons.Should().Be("Modified");
    }

    [Fact]
    public void RunMethodRef()
    {
        var step1 = """
using ConsoleAppFramework;

unsafe
{
    ConsoleApp.Run(args, &DoHello);
}

static void DoHello()
{
}
""";

        var step2 = """
using ConsoleAppFramework;

unsafe
{
    ConsoleApp.Run(args, &DoHello);
    Console.WriteLine("bar"); // unrelated line
}

static void DoHello()
{
    Console.WriteLine("foo"); // body 
}
""";

        var step3 = """
using ConsoleAppFramework;

unsafe
{
    ConsoleApp.Run(args, &DoHello);
    Console.WriteLine("bar");
}

static void DoHello(int x, int y) // change signature
{
    Console.WriteLine("foo");
}
""";

        var reasons = CSharpGeneratorRunner.GetIncrementalGeneratorTrackedStepsReasons("ConsoleApp.Run", step1, step2, step3);

        reasons[0][0].Reasons.Should().Be("New");
        reasons[1][0].Reasons.Should().Be("Unchanged");
        reasons[2][0].Reasons.Should().Be("Modified");
    }
}

