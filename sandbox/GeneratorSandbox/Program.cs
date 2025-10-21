using ConsoleAppFramework;
using GeneratorSandbox;

// fail
//await ConsoleApp.RunAsync(args, Commands.Save);


var app = ConsoleApp.Create();


app.Run(args, true, true, true);


// fail
// await ConsoleApp.RunAsync(args, async () => await Task.Delay(1000, CancellationToken.None));


public class Commands
{
    /// <summary>
    /// Some sort of save command.
    /// </summary>
    public async Task<int> Save(CancellationToken ct)
    {

        await Task.Delay(1000);
        return 0;
    }
}


// `using var posixSignalHandler = PosixSignalHandler.Register(Timeout);`
