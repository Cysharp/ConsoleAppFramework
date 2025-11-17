using ConsoleAppFramework;

// args = ["foo", "--help"];

var app = ConsoleApp.Create();

app.Add("build|b", () => { });
app.Add("test|t", () => { });
app.Add("keyvault|kv", () => { });
app.Add<Commands>();

app.Run(args);

public class Commands
{
    /// <summary>Analyze the current package and report errors, but don't build object files.</summary>
    [Command("check|c")]
    public void Check() { }

    /// <summary>Build this packages's and its dependencies' documenation.</summary>
    [Command("doc|d")]
    public void Doc() { }
}
