using Microsoft.AspNetCore.Hosting;
using System.Linq;
using Microsoft.Extensions.Logging;
using MicroBatchFramework;
using System;
using System.Threading.Tasks;

namespace WebHostingApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await new WebHostBuilder().RunBatchEngineWebHosting("http://localhost:12345");
        }
    }

    public class MyBatch : BatchBase
    {
        public void Foo()
        {
            Context.Logger.LogInformation("foo bar baz");
        }

        public void Sum(int x, int y)
        {
            Context.Logger.LogInformation((x + y).ToString());
        }
        public void SimpleString(string input)
        {
            Context.Logger.LogInformation("In: " + input);
        }
        public void SimpleObject(Person person)
        {
            Context.Logger.LogInformation(person.Name + ":" + person.Age);
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
