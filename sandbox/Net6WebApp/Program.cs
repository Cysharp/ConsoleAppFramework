


var sc = new ServiceCollection();
sc.AddTransient<MyClass>();

#pragma warning disable ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'
var p = sc.BuildServiceProvider();
#pragma warning restore ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'

var b = WebApplication.CreateBuilder();



b.Logging.AddConsole();

//.Services.loggi


//var builder = WebApplication.CreateBuilder(args);
//builder.Logging


var hoge = ActivatorUtilities.CreateInstance(p, typeof(MyClass2), new object[] { 1, 2, "hoge", new MyClass3() });



public class MyClass
{

}

public class MyClass2
{
    public MyClass2(int x, MyClass3 mc3, MyClass mc, string z, int y)
    {
        Console.WriteLine("OK:" + (x, mc, y, mc3, z));
    }
}

public class MyClass3
{
}