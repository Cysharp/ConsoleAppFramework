using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Utf8Json;

namespace MicroBatchFramework
{
    public class BatchEngine
    {
        readonly ILogger<BatchEngine> logger;
        readonly IServiceProvider provider;
        readonly IBatchInterceptor interceptor;
        readonly CancellationToken cancellationToken;

        public BatchEngine(ILogger<BatchEngine> logger, IServiceProvider provider, IBatchInterceptor interceptor, CancellationToken cancellationToken)
        {
            this.logger = logger;
            this.provider = provider;
            this.interceptor = interceptor;
            this.cancellationToken = cancellationToken;
        }

        internal async Task RunAsync(Type type, MethodInfo method, string[] args)
        {
            logger.LogTrace("BatchEngine.Run Start");
            var ctx = new BatchContext(args, DateTime.UtcNow, cancellationToken, logger);
            await RunCore(ctx, type, method, args, 1); // 0 is type selector
        }

        internal async Task RunAsync(Type type, string[] args)
        {
            logger.LogTrace("BatchEngine.Run Start");

            MethodInfo method = null;
            var ctx = new BatchContext(args, DateTime.UtcNow, cancellationToken, logger);
            try
            {
                await interceptor.OnBatchRunBegin(ctx);

                if (type == typeof(void))
                {
                    SetFail(ctx, "Type or method does not found on this Program. args: " + string.Join(" ", args));
                    return;
                }

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (methods.Length != 1)
                {
                    SetFail(ctx, "Method can not select. T of Run/UseBatchEngine<T> have to be contain single method. Type:" + type.FullName);
                    return;
                }
                method = methods[0];
            }
            catch (Exception ex)
            {
                SetFail(ctx, "Fail to get method. Type:" + type.FullName, ex);
                return;
            }

            await RunCore(ctx, type, method, args, 0);
        }

        async Task RunCore(BatchContext ctx, Type type, MethodInfo methodInfo, string[] args, int argsOffset)
        {
            object instance = null;
            object[] invokeArgs = null;

            try
            {
                var argumentDictionary = ParseArgument(args, argsOffset);
                invokeArgs = GetInvokeArguments(methodInfo.GetParameters(), argumentDictionary);
            }
            catch (Exception ex)
            {
                SetFail(ctx, "Fail to match method parameter on " + type.Name + "." + methodInfo.Name + ". args: " + string.Join(" ", args), ex);
                return;
            }

            try
            {
                instance = provider.GetService(type);
                typeof(BatchBase).GetProperty(nameof(BatchBase.Context)).SetValue(instance, ctx);
            }
            catch (Exception ex)
            {
                SetFail(ctx, "Fail to create BatchBase instance. Type:" + type.FullName, ex);
                return;
            }

            try
            {
                var result = methodInfo.Invoke(instance, invokeArgs);
                if (result is Task t)
                {
                    await t;
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException || ex is TaskCanceledException)
                {
                    return; // do nothing
                }

                SetFail(ctx, "Fail in batch running on " + type.Name + "." + methodInfo.Name, ex);
                return;
            }

            await interceptor.OnBatchRunComplete(ctx, null, null);
            logger.LogTrace("BatchEngine.Run Complete Successfully");
        }

        void SetFail(BatchContext context, string message)
        {
            Environment.ExitCode = 1;
            logger.LogError(message);
            interceptor.OnBatchRunComplete(context, message, null);
        }

        void SetFail(BatchContext context, string message, Exception ex)
        {
            Environment.ExitCode = 1;
            logger.LogError(ex, message);
            interceptor.OnBatchRunComplete(context, message, ex);
        }

        static object[] GetInvokeArguments(ParameterInfo[] parameters, ReadOnlyDictionary<string, string> argumentDictionary)
        {
            var invokeArgs = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var item = parameters[i];
                var option = item.GetCustomAttribute<OptionAttribute>();
                if (argumentDictionary.TryGetValue(item.Name, out var value) || argumentDictionary.TryGetValue(option?.ShortName?.TrimStart('-') ?? "", out value))
                {
                    if (parameters[i].ParameterType == typeof(string))
                    {
                        if (!value.StartsWith("\"") && !value.EndsWith("\""))
                        {
                            value = "\"" + value + "\"";
                        }
                    }

                    // decouple dependency?
                    invokeArgs[i] = JsonSerializer.NonGeneric.Deserialize(parameters[i].ParameterType, value);
                }
                else
                {
                    if (item.HasDefaultValue)
                    {
                        invokeArgs[i] = item.DefaultValue;
                    }
                    else
                    {
                        throw new ArgumentException("\"" + item.Name + "\"" + " not found in argument.");
                    }
                }
            }

            return invokeArgs;
        }

        static ReadOnlyDictionary<string, string> ParseArgument(string[] args, int argsOffset)
        {
            var dict = new Dictionary<string, string>(args.Length, StringComparer.OrdinalIgnoreCase);
            for (int i = argsOffset; i < args.Length;)
            {
                var key = args[i++].TrimStart('-');
                if (i < args.Length && !args[i].StartsWith("-"))
                {
                    var value = args[i++];
                    dict.Add(key, value);
                }
                else
                {
                    dict.Add(key, "true"); // boolean switch
                }
            }

            return new ReadOnlyDictionary<string, string>(dict);
        }
    }
}
