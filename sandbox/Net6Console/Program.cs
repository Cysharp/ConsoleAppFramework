using ConsoleAppFramework;
using System.Runtime.InteropServices;


// RegisterShutdownHandlers();

//var r2 = Console.ReadKey();


// Console.ReadKey
ConsoleApp.Run(args, async (ConsoleAppContext ctx) =>
{
    var key = await Task.Run(() => Console.ReadKey(intercept: true)).WaitAsync(ctx.CancellationToken);
    Console.WriteLine(key.KeyChar);
});
