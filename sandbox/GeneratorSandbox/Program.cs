using ConsoleAppFramework;

args = ["--x", "10", "y", "20"]; // missing argument
ConsoleApp.Run(args, (int x, int y) =>
{
    Console.WriteLine(new { x, y });
});
