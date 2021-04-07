using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppFramework.WebHosting
{
    internal class WebHostingFilter : ConsoleAppFilter
    {
        public bool CompleteSuccessfully { get; private set; }
        public Exception? Exception { get; private set; }

        public override async ValueTask Invoke(ConsoleAppContext context, Func<ConsoleAppContext, ValueTask> next)
        {
            try
            {
                await next(context);
                this.CompleteSuccessfully = true;
            }
            catch (Exception ex)
            {
                this.CompleteSuccessfully = false;
                this.Exception = ex;
            }
        }
    }

    internal class LogCollector : ILogger<ConsoleAppEngine>
    {
        readonly ILogger<ConsoleAppEngine> innerLogger;
        readonly StringBuilder sb;

        public LogCollector(ILogger<ConsoleAppEngine> innerLogger)
        {
            this.innerLogger = innerLogger;
            this.sb = new StringBuilder();
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return innerLogger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var msg = formatter(state, exception);
            lock (sb)
            {
                sb.AppendLine(msg);
            }

            innerLogger.Log<TState>(logLevel, eventId, state, exception, formatter);
        }

        public override string ToString()
        {
            lock (sb)
            {
                return sb.ToString();
            }
        }
    }

    public class ConsoleAppFrameworkMiddleware
    {
        readonly RequestDelegate next;
        readonly IServiceProvider provider;
        readonly ILogger<ConsoleAppEngine> logger;
        readonly ConsoleAppOptions options;

        readonly Dictionary<string, MethodInfo> methodLookup;

        public ConsoleAppFrameworkMiddleware(RequestDelegate next, ILogger<ConsoleAppEngine> logger, IServiceProvider provider, TargetConsoleAppTypeCollection targetTypes, ConsoleAppOptions options)
        {
            this.next = next;
            this.logger = logger;
            this.provider = provider;
            this.options = options;
            this.methodLookup = BuildMethodLookup(targetTypes);
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var path = httpContext.Request.Path.Value;
            if (!methodLookup.TryGetValue(path!, out var methodInfo))
            {
                await next(httpContext);
                return;
            }

            // create args
            string?[] args;
            try
            {
                if (httpContext.Request.HasFormContentType)
                {
                    args = new string[(httpContext.Request.Form.Count * 2) + 1];
                    {
                        var i = 0;
                        // MemberInfo.DeclaringType is null only if it is a member of a VB Module.
                        args[i++] = methodInfo.DeclaringType!.Name + "." + methodInfo.Name;
                        foreach (var item in httpContext.Request.Form)
                        {
                            args[i++] = "-" + item.Key;
                            args[i++] = (item.Value.Count == 0) ? null
                                      : (item.Value.Count == 1) ? item.Value[0]
                                      : "[" + string.Join(", ", item.Value) + "]";
                        }
                    }
                }
                else
                {
                    // MemberInfo.DeclaringType is null only if it is a member of a VB Module.
                    args = new[] { methodInfo.DeclaringType!.Name + "." + methodInfo.Name };
                }
            }
            catch (Exception ex)
            {
                httpContext.Response.ContentType = "text/plain";
                httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await httpContext.Response.WriteAsync(ex.ToString());
                return;
            }

            // run with collect statuses
            var collectLogger = new LogCollector(logger);
            var hostingFilter = new WebHostingFilter();
            if (options.GlobalFilters == null)
            {
                options.GlobalFilters = new[] { hostingFilter };
            }
            else
            {
                options.GlobalFilters = options.GlobalFilters.Prepend(hostingFilter).ToArray();
            }

            var engine = new ConsoleAppEngine(collectLogger, provider, options, httpContext.RequestAborted);
            await engine.RunAsync(methodInfo.DeclaringType, methodInfo, args);

            // out result
            if (hostingFilter.CompleteSuccessfully)
            {
                httpContext.Response.ContentType = "text/plain";
                httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
                await httpContext.Response.WriteAsync(collectLogger.ToString());
            }
            else
            {
                var errorMsg = (hostingFilter.Exception != null) ? hostingFilter.Exception.ToString() : "";
                httpContext.Response.ContentType = "text/plain";
                httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await httpContext.Response.WriteAsync(errorMsg);
            }
        }

        static Dictionary<string, MethodInfo> BuildMethodLookup(IEnumerable<Type> consoleAppTypes)
        {
            var methods = new Dictionary<string, MethodInfo>();

            foreach (var type in consoleAppTypes)
            {
                foreach (var item in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    methods.Add("/" + type.Name + "/" + item.Name, item);
                }
            }

            return methods;
        }
    }
}