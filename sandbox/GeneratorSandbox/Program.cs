using ConsoleAppFramework;

var app = ConsoleApp.Create();

app.ConfigureServices(_ => { Console.WriteLine("ConfigureServices"); });
app.Add("test", () => Console.WriteLine("test output"));

for (var i = 0; i < 3; i++)
{
    app.Run(["test"], startHost: false, stopHost: false, disposeServiceProvider: false);
}


