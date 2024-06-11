using ConsoleAppFramework;

var app = ConsoleApp.Create();

app.Add("aaa", () =>
{
});

app.Add("aaa", async Task<int> () =>
{
    await Task.Yield();
    return default!; 
});

app.Run(args);
