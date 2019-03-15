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

        public async Task RunAsync(Type type, MethodInfo method, string[] args)
        {
            logger.LogTrace("BatchEngine.Run Start");
            var ctx = new BatchContext(args, DateTime.UtcNow, cancellationToken, logger);
            await RunCore(ctx, type, method, args, 1); // 0 is type selector
        }

        public async Task RunAsync(Type type, string[] args)
        {
            logger.LogTrace("BatchEngine.Run Start");

            int argsOffset = 0;
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
                foreach (var item in methods)
                {
                    var command = item.GetCustomAttribute<CommandAttribute>();
                    if (command != null)
                    {
                        if (args.Length > 0 && command.EqualsAny(args[0]))
                        {
                            method = item;
                            argsOffset = 1;
                            goto RUN;
                        }
                    }
                    else
                    {
                        if (method != null)
                        {
                            goto FAIL;
                        }
                        method = item;
                    }
                }

                if (method != null)
                {
                    goto RUN;
                }

            FAIL:
                await SetFailAsync(ctx, "Method can not select. T of Run/UseBatchEngine<T> have to be contain single method or command. Type:" + type.FullName);
                return;
            }
            catch (Exception ex)
            {
                await SetFailAsync(ctx, "Fail to get method. Type:" + type.FullName, ex);
                return;
            }

        RUN:
            await RunCore(ctx, type, method, args, argsOffset);
        }

        async Task RunCore(BatchContext ctx, Type type, MethodInfo methodInfo, string[] args, int argsOffset)
        {
            object instance = null;
            object[] invokeArgs = null;

            try
            {
                if (!TryGetInvokeArguments(methodInfo.GetParameters(), args, argsOffset, out invokeArgs, out var errorMessage))
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
                    return;
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

        static bool TryGetInvokeArguments(ParameterInfo[] parameters, string[] args, int argsOffset, out object[] invokeArgs, out string errorMessage)
        {
            var argumentDictionary = ParseArgument(args, argsOffset);
            invokeArgs = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var item = parameters[i];
                var option = item.GetCustomAttribute<OptionAttribute>();

                var value = default(OptionParameter);
                if (option != null && option.Index != -1)
                {
                    value = new OptionParameter { Value = args[argsOffset + i] };
                }

                if (value.Value != null || argumentDictionary.TryGetValue(item.Name, out value) || argumentDictionary.TryGetValue(option?.ShortName?.TrimStart('-') ?? "", out value))
                {
                    if (parameters[i].ParameterType == typeof(bool) && value.Value == null)
                    {
                        invokeArgs[i] = value.BooleanSwitch;
                        continue;
                    }

                    if (value.Value != null)
                    {
                        if (parameters[i].ParameterType == typeof(string))
                        {
                            // when string, invoke directly(avoid JSON escape)
                            invokeArgs[i] = value.Value;
                            continue;
                        }
                        else if (parameters[i].ParameterType.IsEnum)
                        {
                            try
                            {
                                invokeArgs[i] = Enum.Parse(parameters[i].ParameterType, value.Value, true);
                                continue;
                            }
                            catch
                            {
                                errorMessage = "Parameter \"" + item.Name + "\"" + " fail on Enum parsing.";
                                return false;
                            }
                        }
                        else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(parameters[i].ParameterType))
                        {
                            var v = value.Value;
                            if (!(v.StartsWith("[") && v.EndsWith("]")))
                            {
                                v = "[" + v + "]";
                            }
                            try
                            {
                                invokeArgs[i] = JsonSerializer.NonGeneric.Deserialize(parameters[i].ParameterType, v);
                                continue;
                            }
                            catch
                            {
                                errorMessage = "Parameter \"" + item.Name + "\"" + " fail on JSON deserialize, plaease check type or JSON escape or add double-quotation.";
                                return false;
                            }
                        }
                        else
                        {
                            // decouple dependency?
                            try
                            {
                                invokeArgs[i] = JsonSerializer.NonGeneric.Deserialize(parameters[i].ParameterType, value.Value);
                                continue;
                            }
                            catch
                            {
                                errorMessage = "Parameter \"" + item.Name + "\"" + " fail on JSON deserialize, plaease check type or JSON escape or add double-quotation.";
                                return false;
                            }
                        }
                    }
                }

                if (item.HasDefaultValue)
                {
                    invokeArgs[i] = item.DefaultValue;
                }
                else
                {
                    errorMessage = "Required parameter \"" + item.Name + "\"" + " not found in argument.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        static ReadOnlyDictionary<string, OptionParameter> ParseArgument(string[] args, int argsOffset)
        {
            var dict = new Dictionary<string, OptionParameter>(args.Length, StringComparer.OrdinalIgnoreCase);
            for (int i = argsOffset; i < args.Length;)
            {
                if (!args[i].StartsWith("-"))
                {
                    i++;
                    continue; // not key
                }

                var key = args[i++].TrimStart('-');
                if (i < args.Length && !args[i].StartsWith("-"))
                {
                    var value = args[i++];
                    dict.Add(key, new OptionParameter { Value = value });
                }
                else
                {
                    dict.Add(key, new OptionParameter { BooleanSwitch = true });
                }
            }

            return new ReadOnlyDictionary<string, OptionParameter>(dict);
        }

        struct OptionParameter
        {
            public string Value;
            public bool BooleanSwitch;
        }
    }
}
