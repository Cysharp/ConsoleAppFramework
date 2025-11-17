using ConsoleAppFramework;

var app = ConsoleApp.Create();

app.Add("build|b", () => { });
app.Add("keyvault|kv", () => { });
app.Add<Commands>();

app.Run(args);

public class Commands
{
    /// <summary>
    /// Executes the check command using the specified coordinates.
    /// </summary>
    [Command("check|c")]
    public void Check() { }

    /// <summary>Build this packages's and its dependencies' documenation.</summary>
    [Command("doc|d")]
    public void Doc() { }
}
