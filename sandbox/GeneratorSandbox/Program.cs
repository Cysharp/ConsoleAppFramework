using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

// Use SigNoz profiling: (view: http://localhost:8080/ )
// git clone https://github.com/SigNoz/signoz.git
// cd signoz/deploy/docker
// docker compose up
Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317"); // 4317 or 4318

// crate builder from Microsoft.Extensions.Hosting.Host
var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

builder.Services.AddOpenTelemetry()
    .UseOtlpExporter()
    .ConfigureResource(resource =>
    {
        resource.AddService("ConsoleAppFramework Telemetry Sample");
    })
    .WithMetrics(metrics =>
    {
        // configure for metrics
        metrics.AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation();
    })
    .WithTracing(tracing =>
    {
        // configure for tracing
        tracing.SetSampler(new AlwaysOnSampler())
            .AddHttpClientInstrumentation()
            .AddSource(ConsoleAppFrameworkSampleActivitySource.Name);
    })
    .WithLogging(logging =>
    {
        // configure for logging
    });

var app = builder.ToConsoleAppBuilder();

app.Add<SampleCommand>();

// setup filter
app.UseFilter<CommandTracingFilter>();

await app.RunAsync(args); // Run

public class SampleCommand(ILogger<SampleCommand> logger)
{
    [Command("")]
    public async Task Run(CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();

        var ms = await httpClient.GetStringAsync("https://www.microsoft.com", cancellationToken);
        var google = await httpClient.GetStringAsync("https://www.google.com", cancellationToken);

        logger.LogInformation("Sequential Query done.");

        var ms2 = httpClient.GetStringAsync("https://www.microsoft.com", cancellationToken);
        var google2 = httpClient.GetStringAsync("https://www.google.com", cancellationToken);
        var apple2 = httpClient.GetStringAsync("https://www.apple.com", cancellationToken);
        await Task.WhenAll(ms2, google2, apple2);

        logger.LogInformation("Parallel Query done.");
    }
}



public static class ConsoleAppFrameworkSampleActivitySource
{
    public const string Name = "ConsoleAppFrameworkSample";

    public static ActivitySource Instance { get; } = new ActivitySource(Name);
}

// Sample Activity filter
internal class CommandTracingFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        using var activity = ConsoleAppFrameworkSampleActivitySource.Instance.StartActivity("CommandStart");

        if (activity == null) // Telemtry is not listened
        {
            await Next.InvokeAsync(context, cancellationToken);
        }
        else
        {
            activity.SetTag("console_app.command_name", context.CommandName);
            activity.SetTag("console_app.command_args", string.Join(" ", context.EscapedArguments));

            try
            {
                await Next.InvokeAsync(context, cancellationToken);
                activity.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    activity.SetStatus(ActivityStatusCode.Error, "Canceled");
                }
                else
                {
                    activity.AddException(ex);
                    activity.SetStatus(ActivityStatusCode.Error);
                }
                throw;
            }
        }
    }
}
