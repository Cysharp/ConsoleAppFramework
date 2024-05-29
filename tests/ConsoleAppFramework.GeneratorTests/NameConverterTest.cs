using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace ConsoleAppFramework.GeneratorTests;

public class NameConverterTest(ITestOutputHelper output)
{
    VerifyHelper verifier = new VerifyHelper(output, "CAF");

    [Fact]
    public void KebabCase()
    {
        NameConverter.ToKebabCase("").Should().Be("");
        NameConverter.ToKebabCase("HelloWorld").Should().Be("hello-world");
        NameConverter.ToKebabCase("HelloWorldMyHome").Should().Be("hello-world-my-home");
        NameConverter.ToKebabCase("helloWorld").Should().Be("hello-world");
        NameConverter.ToKebabCase("hello-world").Should().Be("hello-world");
        NameConverter.ToKebabCase("A").Should().Be("a");
        NameConverter.ToKebabCase("AB").Should().Be("ab");
        NameConverter.ToKebabCase("ABC").Should().Be("abc");
        NameConverter.ToKebabCase("ABCD").Should().Be("abcd");
        NameConverter.ToKebabCase("ABCDeF").Should().Be("abc-def");
        NameConverter.ToKebabCase("XmlReader").Should().Be("xml-reader");
        NameConverter.ToKebabCase("XMLReader").Should().Be("xml-reader");
        NameConverter.ToKebabCase("MLLibrary").Should().Be("ml-library");
    }

    [Fact]
    public void CommmandName()
    {
        verifier.Execute("""
var builder = ConsoleApp.CreateBuilder();
builder.Add<MyClass>();
builder.Run(args);

public class MyClass
{
    public void HelloWorld()
    {
        Console.Write("Hello World!");
    }
}
""", args: "hello-world", expected: "Hello World!");
    }

    [Fact]
    public void OptionName()
    {
        verifier.Execute("""
var builder = ConsoleApp.CreateBuilder();
builder.Add<MyClass>();
builder.Run(args);

public class MyClass
{
    public void HelloWorld(string fooBar)
    {
        Console.Write("Hello World! " + fooBar);
    }
}
""", args: "hello-world --foo-bar aiueo", expected: "Hello World! aiueo");


        verifier.Execute("""
var builder = ConsoleApp.CreateBuilder();
var mc = new MyClass();
builder.Add("hello-world", mc.HelloWorld);
builder.Run(args);

public class MyClass
{
    public void HelloWorld(string fooBar)
    {
        Console.Write("Hello World! " + fooBar);
    }
}
""", args: "hello-world --foo-bar aiueo", expected: "Hello World! aiueo");
    }
}
