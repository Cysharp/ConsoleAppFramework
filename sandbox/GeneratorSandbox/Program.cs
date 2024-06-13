using ConsoleAppFramework;


var t = new Test();

var app = ConsoleApp.Create();

app.Add("foo", t.Handle);

app.Run(args);


public partial class Test
{
    public void Handle(
        bool a1,
        bool a2,
        bool a3,
        bool a4,
        bool a5,
        bool a6,
        bool a7,
        bool a8,
        bool a9,
        bool a10,
        bool a11,
        bool a12,
        bool a13,
        bool a14,
        bool a15,
        bool a16,
        bool a17,
        bool a18,
        bool a19,
        string foo = null
    )
    {
        Console.Write("ok");
    }
}