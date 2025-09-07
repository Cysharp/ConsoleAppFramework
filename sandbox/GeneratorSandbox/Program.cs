using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddService("ConsoleAppFramework Telemetry Sample");
    })
    .WithMetrics(metrics =>
    {
        metrics.AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.SetSampler(new AlwaysOnSampler())
            .AddHttpClientInstrumentation()
            .AddSource(ConsoleAppFrameworkSampleActivitySource.Name)
            .AddOtlpExporter();
    })
    .WithLogging(logging =>
    {
        logging.AddOtlpExporter();
    });

var app = builder.ToConsoleAppBuilder();

app.UseFilter<CommandTracingFilter>();

app.Add("", async ([FromServices] ILogger<Program> logger) =>
{
    using var httpClient = new HttpClient();
    var ms = await httpClient.GetStringAsync("https://www.microsoft.com");
    logger.LogInformation(ms);
    var google = await httpClient.GetStringAsync("https://www.google.com");
    logger.LogInformation(google);

    var ms2 = httpClient.GetStringAsync("https://www.microsoft.com");
    var google2 = httpClient.GetStringAsync("https://www.google.com");
    var apple2 = httpClient.GetStringAsync("https://www.apple.com");
    await Task.WhenAll(ms2, google2, apple2);

    logger.LogInformation(apple2.Result);

    logger.LogInformation("OK");
});

await app.RunAsync(args);

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
        if (activity != null)
        {
            activity.SetTag("console_app.command_name", context.CommandName);
            activity.AddBaggage("console_app.command_args", string.Join(" ", context.EscapedArguments));
        }

        try
        {
            await Next.InvokeAsync(context, cancellationToken);
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }
        }
        catch (Exception ex)
        {
            if (activity != null)
            {
                activity.AddException(ex);
                activity.SetStatus(ActivityStatusCode.Error);
            }
            throw;
        }
    }
}
