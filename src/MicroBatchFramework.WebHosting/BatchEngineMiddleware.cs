using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MicroBatchFramework.WebHosting
{
    internal class WebHostingInterceptor : IBatchInterceptor
    {
        readonly IBatchInterceptor innerInterceptor;

        public bool CompleteSuccessfully { get; private set; }
        public string ErrorMessage { get; private set; }
        public Exception Exception { get; private set; }

        public WebHostingInterceptor(IBatchInterceptor innerInterceptor)
        {
            this.innerInterceptor = innerInterceptor;
        }

        public ValueTask OnBatchEngineBeginAsync(IServiceProvider serviceProvider, ILogger<BatchEngine> logger)
        {
            return innerInterceptor.OnBatchEngineBeginAsync(serviceProvider, logger);
        }

        public ValueTask OnBatchEngineEndAsync()
        {
            return innerInterceptor.OnBatchEngineEndAsync();
        }

        public ValueTask OnBatchRunBeginAsync(BatchContext context)
        {
            return innerInterceptor.OnBatchRunBeginAsync(context);
        }

        public ValueTask OnBatchRunCompleteAsync(BatchContext context, string errorMessageIfFailed, Exception exceptionIfExists)
        {
            this.CompleteSuccessfully = (errorMessageIfFailed == null && exceptionIfExists == null);
            this.ErrorMessage = errorMessageIfFailed;
            this.Exception = exceptionIfExists;
            return innerInterceptor.OnBatchRunCompleteAsync(context, errorMessageIfFailed, exceptionIfExists);
        }
    }

    internal class LogCollector : ILogger<BatchEngine>
    {
        readonly ILogger<BatchEngine> innerLogger;
        readonly StringBuilder sb;

        public LogCollector(ILogger<BatchEngine> innerLogger)
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

    public class BatchEngineMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IServiceProvider provider;
        private readonly ILogger<BatchEngine> logger;
        private readonly IBatchInterceptor interceptor;

        private readonly Dictionary<string, MethodInfo> methodLookup;

        public BatchEngineMiddleware(RequestDelegate next, ILogger<BatchEngine> logger, IBatchInterceptor interceptor, IServiceProvider provider, TargetBatchTypeCollection targetTypes)
        {
            this.next = next;
            this.logger = logger;
            this.interceptor = interceptor;
            this.provider = provider;
            this.methodLookup = BuildMethodLookup(targetTypes);
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var path = httpContext.Request.Path.Value;
            if (!methodLookup.TryGetValue(path, out var methodInfo))
            {
                await next(httpContext);
                return;
            }

            // create args
            string[] args = null;
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
            var hostingInterceptor = new WebHostingInterceptor(interceptor);
            var collectLogger = new LogCollector(logger);

            var engine = new BatchEngine(collectLogger, provider, hostingInterceptor, httpContext.RequestAborted);
            await engine.RunAsync(methodInfo.DeclaringType, methodInfo, args);

            // out result
            if (hostingInterceptor.CompleteSuccessfully)
            {
                httpContext.Response.ContentType = "text/plain";
                httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
                await httpContext.Response.WriteAsync(collectLogger.ToString());
            }
            else
            {
                var errorMsg = ((hostingInterceptor.ErrorMessage != null) ? hostingInterceptor.ErrorMessage + Environment.NewLine : "")
                             + ((hostingInterceptor.Exception != null) ? hostingInterceptor.Exception.ToString() : "");
                httpContext.Response.ContentType = "text/plain";
                httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await httpContext.Response.WriteAsync(errorMsg);
            }
        }

        static Dictionary<string, MethodInfo> BuildMethodLookup(IEnumerable<Type> batchTypes)
        {
            var methods = new Dictionary<string, MethodInfo>();

            foreach (var type in batchTypes)
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