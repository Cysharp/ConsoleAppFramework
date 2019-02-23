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
                await interceptor.OnBatchRunBeginAsync(ctx);

                if (type == typeof(void))
                {
                    await SetFailAsync(ctx, "Type or method does not found on this Program. args: " + string.Join(" ", args));
                    return;
                }

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (methods.Length != 1)
                {
                    await SetFailAsync(ctx, "Method can not select. T of Run/UseBatchEngine<T> have to be contain single method. Type:" + type.FullName);
                    return;
                }
                method = methods[0];
            }
            catch (Exception ex)
            {
                await SetFailAsync(ctx, "Fail to get method. Type:" + type.FullName, ex);
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
                if (!TryGetInvokeArguments(methodInfo.GetParameters(), argumentDictionary, out invokeArgs, out var errorMessage))
                {
                    await SetFailAsync(ctx, errorMessage + " args: " + string.Join(" ", args));
                    return;
                }
            }
            catch (Exception ex)
            {
                await SetFailAsync(ctx, "Fail to match method parameter on " + type.Name + "." + methodInfo.Name + ". args: " + string.Join(" ", args), ex);
                return;
            }

            try
            {
                instance = provider.GetService(type);
                typeof(BatchBase).GetProperty(nameof(BatchBase.Context)).SetValue(instance, ctx);
            }
            catch (Exception ex)
            {
                await SetFailAsync(ctx, "Fail to create BatchBase instance. Type:" + type.FullName, ex);
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

                if (ex is TargetInvocationException tex)
                {
                    await SetFailAsync(ctx, "Fail in batch running on " + type.Name + "." + methodInfo.Name, tex.InnerException);
                    return;
                }
                else
                {
                    await SetFailAsync(ctx, "Fail in batch running on " + type.Name + "." + methodInfo.Name, ex);
                }
            }

            await interceptor.OnBatchRunCompleteAsync(ctx, null, null);
            logger.LogTrace("BatchEngine.Run Complete Successfully");
        }

        async ValueTask SetFailAsync(BatchContext context, string message)
        {
            Environment.ExitCode = 1;
            logger.LogError(message);
            await interceptor.OnBatchRunCompleteAsync(context, message, null);
        }

        async ValueTask SetFailAsync(BatchContext context, string message, Exception ex)
        {
            Environment.ExitCode = 1;
            logger.LogError(ex, message);
            await interceptor.OnBatchRunCompleteAsync(context, message, ex);
        }

        static bool TryGetInvokeArguments(ParameterInfo[] parameters, ReadOnlyDictionary<string, string> argumentDictionary, out object[] invokeArgs, out string errorMessageIfNotFound)
        {
            invokeArgs = new object[parameters.Length];

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
                        errorMessageIfNotFound = "Required parameter \"" + item.Name + "\"" + " not found in argument.";
                        return false;
                    }
                }
            }

            errorMessageIfNotFound = null;
            return true;
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
