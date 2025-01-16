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
        NameConverter.ToKebabCase("").ShouldBe("");
        NameConverter.ToKebabCase("HelloWorld").ShouldBe("hello-world");
        NameConverter.ToKebabCase("HelloWorldMyHome").ShouldBe("hello-world-my-home");
        NameConverter.ToKebabCase("helloWorld").ShouldBe("hello-world");
        NameConverter.ToKebabCase("hello-world").ShouldBe("hello-world");
        NameConverter.ToKebabCase("A").ShouldBe("a");
        NameConverter.ToKebabCase("AB").ShouldBe("ab");
        NameConverter.ToKebabCase("ABC").ShouldBe("abc");
        NameConverter.ToKebabCase("ABCD").ShouldBe("abcd");
        NameConverter.ToKebabCase("ABCDeF").ShouldBe("abc-def");
        NameConverter.ToKebabCase("XmlReader").ShouldBe("xml-reader");
        NameConverter.ToKebabCase("XMLReader").ShouldBe("xml-reader");
        NameConverter.ToKebabCase("MLLibrary").ShouldBe("ml-library");
    }

    [Fact]
    public void CommmandName()
    {
        verifier.Execute("""
var builder = ConsoleApp.Create();
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
var builder = ConsoleApp.Create();
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
var builder = ConsoleApp.Create();
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
