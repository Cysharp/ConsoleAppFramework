using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppFramework.GeneratorTests;

[ClassDataSource<VerifyHelper>]
public class GeneratorOptionsTest(VerifyHelper verifier)
{
    [Test]
    public async Task DisableNamingConversionRun()
    {
        await verifier.Execute("""
[assembly: ConsoleAppFrameworkGeneratorOptions(DisableNamingConversion = true)]

ConsoleApp.Run(args, (int fooBarBaz) => { Console.Write(fooBarBaz); });
""", args: "--fooBarBaz 100", expected: "100");

        await verifier.Execute("""
[assembly: ConsoleAppFrameworkGeneratorOptions(DisableNamingConversion = false)]

ConsoleApp.Run(args, (int fooBarBaz) => { Console.Write(fooBarBaz); });
""", args: "--foo-bar-baz 100", expected: "100");
    }

    [Test]
    public async Task DisableNamingConversionBuilder()
    {
        await verifier.Execute("""
[assembly: ConsoleAppFrameworkGeneratorOptions(DisableNamingConversion = true)]

var app = ConsoleApp.Create();
app.Add<Commands>();
app.Run(args);

class Commands
{
    public async Task FooBarBaz(int hogeMoge, int takoYaki)
    {
        Console.Write(hogeMoge + takoYaki);
    }
}
""", args: "FooBarBaz --hogeMoge 100 --takoYaki 200", expected: "300");

        await verifier.Execute("""
[assembly: ConsoleAppFrameworkGeneratorOptions(DisableNamingConversion = false)]

var app = ConsoleApp.Create();
app.Add<Commands>();
app.Run(args);

class Commands
{
    public async Task FooBarBaz(int hogeMoge, int takoYaki)
    {
        Console.Write(hogeMoge + takoYaki);
    }
}
""", args: "foo-bar-baz --hoge-moge 100 --tako-yaki 200", expected: "300");
    }
}
