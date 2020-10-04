using Microsoft.AspNetCore.Hosting;
using System.Linq;
using Microsoft.Extensions.Logging;
using ConsoleAppFramework;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace WebHostingApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
                .RunConsoleAppFrameworkWebHostingAsync("http://localhost:12345");
        }
    }

    public class MyBatch : ConsoleAppBase
    {
        public void Foo()
        {
            Context.Logger.LogInformation("foo bar baz");
        }

        [Command("", "my descp")]
        public void Bar(bool a, bool b = false, bool c = true)
        {
            Context.Logger.LogInformation((a, b, c).ToString());
        }

        public void Sum(int x, int y)
        {
            Context.Logger.LogInformation((x + y).ToString());
        }
        public void SimpleInt(int input)
        {
            Context.Logger.LogInformation("In: " + input);
        }
        public void SimpleString(string input)
        {
            Context.Logger.LogInformation("in is null?: " + (input == null));
            Context.Logger.LogInformation("In: " + input);
        }
        public void SimpleObject(Person person)
        {
            Context.Logger.LogInformation(person.Name + ":" + person.Age);
        }

        public void SimpleEnum(MyFruit fruit)
        {
            Context.Logger.LogInformation(fruit.ToString());
        }

        public void InOut(string input, Person person)
        {
            Context.Logger.LogInformation(person.Name + ":" + person.Age);
            Context.Logger.LogInformation("In: " + input);
        }

        public void SimpleArray(int[] simpleArray)
        {
            Context.Logger.LogInformation(string.Join(", ", simpleArray));
        }

        public void StringArray(string[] simpleArray)
        {
            Context.Logger.LogInformation(string.Join(", ", simpleArray));
        }
        public void OjectArray(Person[] objectArray)
        {
            Context.Logger.LogInformation(string.Join(", ", objectArray.Select(x => (x == null) ? "nul" : x.Age + ":" + x.Name)));
        }

        public void DefaultV(int x = 100, int y = 200, string foo = null)
        {
            Context.Logger.LogInformation("X: " + x);
            Context.Logger.LogInformation("Y: " + y);
            Context.Logger.LogInformation("Foo: " + foo);
        }

        public void Array(int[] intArray, string[] stringArray, Person[] objectArray, MyFruit[] fruits)
        {
            Context.Logger.LogInformation(string.Join(", ", intArray));
            Context.Logger.LogInformation(string.Join(", ", stringArray));
            Context.Logger.LogInformation(string.Join(", ", objectArray.Select(x => x.Age + ":" + x.Name)));
            Context.Logger.LogInformation(string.Join(", ", fruits.Select(x => x.ToString())));
        }

        public void ErrorMan()
        {
            throw new Exception("foo bar baz");
        }
    }

    public enum MyFruit
    {
        Apple, Orange, Grape
    }

    public class Person
    {
        public int Age { get; set; }
        public string Name { get; set; }
    }
}
