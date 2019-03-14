using Microsoft.AspNetCore.Hosting;
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

        public void InOut(string input, Person person)
        {
            Context.Logger.LogInformation(person.Name + ":" + person.Age);
            Context.Logger.LogInformation("In: " + input);
        }

        public void ErrorMan()
        {
            throw new Exception("foo bar baz");
        }
    }

    public class Person
    {
        public int Age { get; set; }
        public string Name { get; set; }
    }
}
