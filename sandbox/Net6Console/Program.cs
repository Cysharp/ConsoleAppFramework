using ConsoleAppFramework;
using System.Runtime.InteropServices;


// RegisterShutdownHandlers();

//var r2 = Console.ReadKey();


// Console.ReadKey
ConsoleApp.Run(args, async (ConsoleAppContext ctx) =>
{
    var key = await Task.Run(() => Console.ReadKey(intercept: true), ctx.CancellationToken);
    Console.WriteLine(key.KeyChar);
    //while (true)
    //{
    //    var r = Console.ReadLine();

    //    Console.WriteLine(r == null);

    //    Console.WriteLine("end:" + ctx.CancellationToken.IsCancellationRequested);
    //}

});
