using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


args = ["--x", "3", "--y", "5"];

// args = ["--help"];

var app = ConsoleApp.Create();

app.Add("", (int x, int y) => { throw new Exception(); });

app.ConfigureLogging(x =>
{
    x.SetMinimumLevel(LogLevel.Trace);
    x.AddSimpleConsole();
});

app.PostConfigureServices((serviceProvider) =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    ConsoleApp.Log = msg => logger.LogInformation(msg);
    ConsoleApp.LogError = msg => logger.LogError(msg);
});

app.Run(args);
