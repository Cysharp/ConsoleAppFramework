using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

args = new[] { "test" };

var builder = ConsoleApp.CreateBuilder(args);
builder.ConfigureServices(x =>
{
    x.AddSingleton(typeof(ISingletonPublisher<>), typeof(MessageBroker<>));
});


var app = builder.Build();

app.AddCommand("test", (ISingletonPublisher<string> logger) =>
{
    Console.WriteLine("OK");
});

app.Run();

public interface IPublisher<TMessage>
{
    void Publish(TMessage message);
}
public interface ISingletonPublisher<TMessage> : IPublisher<TMessage> { }

public class MessageBroker<TMessage> : IPublisher<TMessage>
{
    public void Publish(TMessage message)
    {
        throw new NotImplementedException();
    }
}
public class SingletonMessageBroker<TMessage> : MessageBroker<TMessage>
{
}
