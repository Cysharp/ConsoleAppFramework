using ConsoleAppFramework;

var app = ConsoleApp.Create();

app.Add("foo", () => { });
app.Add("fooa", () => { });

app.Add("choofooaiueo", (int z) => { });

app.Add("Y", Task<int> (int x, int y, int z) => {  });




app.Run(args);
