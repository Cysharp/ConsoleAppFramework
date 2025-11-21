namespace ConsoleAppFramework.GeneratorTests;

public class NameConverterTest
{
    VerifyHelper verifier = new VerifyHelper("CAF");

    [Test]
    public async Task KebabCase()
    {
        await Assert.That(NameConverter.ToKebabCase("")).IsEqualTo("");
        await Assert.That(NameConverter.ToKebabCase("HelloWorld")).IsEqualTo("hello-world");
        await Assert.That(NameConverter.ToKebabCase("HelloWorldMyHome")).IsEqualTo("hello-world-my-home");
        await Assert.That(NameConverter.ToKebabCase("helloWorld")).IsEqualTo("hello-world");
        await Assert.That(NameConverter.ToKebabCase("hello-world")).IsEqualTo("hello-world");
        await Assert.That(NameConverter.ToKebabCase("A")).IsEqualTo("a");
        await Assert.That(NameConverter.ToKebabCase("AB")).IsEqualTo("ab");
        await Assert.That(NameConverter.ToKebabCase("ABC")).IsEqualTo("abc");
        await Assert.That(NameConverter.ToKebabCase("ABCD")).IsEqualTo("abcd");
        await Assert.That(NameConverter.ToKebabCase("ABCDeF")).IsEqualTo("abc-def");
        await Assert.That(NameConverter.ToKebabCase("XmlReader")).IsEqualTo("xml-reader");
        await Assert.That(NameConverter.ToKebabCase("XMLReader")).IsEqualTo("xml-reader");
        await Assert.That(NameConverter.ToKebabCase("MLLibrary")).IsEqualTo("ml-library");
    }

    [Test]
    public async Task CommandName()
    {
        await verifier.Execute("""
var builder = ConsoleApp.Create();
builder.Add<MyClass>();
builder.Run(args);

public class MyClass
{
    public async Task HelloWorld()
    {
        Console.Write("Hello World!");
    }
}
""", args: "hello-world", expected: "Hello World!");
    }

    [Test]
    public async Task OptionName()
    {
        await verifier.Execute("""
var builder = ConsoleApp.Create();
builder.Add<MyClass>();
builder.Run(args);

public class MyClass
{
    public async Task HelloWorld(string fooBar)
    {
        Console.Write("Hello World! " + fooBar);
    }
}
""", args: "hello-world --foo-bar aiueo", expected: "Hello World! aiueo");


        await verifier.Execute("""
var builder = ConsoleApp.Create();
var mc = new MyClass();
builder.Add("hello-world", mc.HelloWorld);
builder.Run(args);

public class MyClass
{
    public async Task HelloWorld(string fooBar)
    {
        Console.Write("Hello World! " + fooBar);
    }
}
""", args: "hello-world --foo-bar aiueo", expected: "Hello World! aiueo");
    }
}
