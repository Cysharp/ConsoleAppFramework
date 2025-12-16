using ConsoleAppFramework;

args = ["write", "--help"];

var app = ConsoleApp.Create();

app.Add("write", (Target target) => { });

app.Run(args);

public enum Target
{
    File,
    Network
}
