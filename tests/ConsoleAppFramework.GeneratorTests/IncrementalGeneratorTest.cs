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
    public void CheckIncrementalStep()
    {
        var step1 = """
using ConsoleAppFramework;

ConsoleApp.Run(args, () => { });
""";

        var step2 = """
using ConsoleAppFramework;

ConsoleApp.Run(args, () => { });

Console.WriteLine("foo"); // unrelated line
""";

        var step3 = """
using ConsoleAppFramework;

ConsoleApp.Run(args, (int x, int y) => { }); // change signature

Console.WriteLine("foo"); // unrelated line
""";

        CSharpGeneratorRunner.CheckIncrementalGenerator(step1, step2, step3);
    }
}
