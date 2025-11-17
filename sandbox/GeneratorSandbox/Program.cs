using ConsoleAppFramework;

args = "9999 --x 1000".Split(' ');

ConsoleApp.Run(args, (int x, [Argument] int y) =>
{
    Console.WriteLine((x, y));
});
