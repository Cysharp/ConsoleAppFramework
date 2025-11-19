using ConsoleAppFramework;


args = ["--x", "3", "--y", "5"];

await ConsoleApp.RunAsync(args, async (int x, int y) => { });

static void Foo(int x, int y)
{
    Console.WriteLine(x + y);
}
