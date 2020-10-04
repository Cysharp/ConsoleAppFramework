using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleAppFramework
{
    public class ConsoleAppEngine
    {
        readonly ILogger<ConsoleAppEngine> logger;
        readonly IServiceProvider provider;
        readonly CancellationToken cancellationToken;
        readonly ConsoleAppOptions options;
        readonly bool isStrict;

        public ConsoleAppEngine(ILogger<ConsoleAppEngine> logger, IServiceProvider provider, ConsoleAppOptions options, CancellationToken cancellationToken)
        {
            this.logger = logger;
            this.provider = provider;
            this.cancellationToken = cancellationToken;
            this.options = options;
            this.isStrict = this.options.StrictOption;
        }

        public async Task RunAsync(Type type, MethodInfo method, string?[] args)
        {
            logger.LogTrace("ConsoleAppEngine.Run Start");
            await RunCore(type, method, args, 1); // 0 is type selector
        }

        public async Task RunAsync(Type type, string[] args)
        {
            logger.LogTrace("ConsoleAppEngine.Run Start");

            int argsOffset = 0;
            MethodInfo? method = null;
            try
            {
                if (type == typeof(void))
                {
                    await SetFailAsync("Type or method does not found on this Program. args: " + string.Join(" ", args));
                    return;
                }

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (methods.Length == 0)
                {
                    await SetFailAsync("Method can not select. T of Run/UseConsoleAppEngine<T> have to be contain single method or command. Type:" + type.FullName);
                    return;
                }

                MethodInfo? helpMethod = null;
                foreach (var item in methods)
                {
                    var command = item.GetCustomAttribute<CommandAttribute>();
                    if (command != null)
                    {
                        if (args.Length > 0 && command.EqualsAny(args[0]))
                        {
                            // command's priority is first
                            method = item;
                            argsOffset = 1;
                            goto RUN;
                        }
                        else
                        {
                            if (command.EqualsAny("help"))
                            {
                                helpMethod = item;
                            }
                        }
                    }
                    else
                    {
                        if (method != null)
                        {
                            await SetFailAsync("Found more than one public methods(without command). Type:" + type.FullName + " Method:" + method.Name + " and " + item.Name);
                            return;
                        }
                        method = item; // found single public(non-command) method.
                    }
                }

                if (method != null)
                {
                    goto RUN;
                }

                // completely not found, invalid command name show help.
                if (helpMethod != null)
                {
                    method = helpMethod;
                    goto RUN;
                }
                else
                {
                    Console.Write(new CommandHelpBuilder(null, options.StrictOption, options.ShowDefaultCommand).BuildHelpMessage(methods, null));
                    return;
                }
            }
            catch (Exception ex)
            {
                await SetFailAsync("Fail to get method. Type:" + type.FullName, ex);
                return;
            }

            RUN:
            await RunCore(type, method, args, argsOffset);
        }

        async Task RunCore(Type type, MethodInfo methodInfo, string?[] args, int argsOffset)
        {
            object instance;
            object[] invokeArgs;

            try
            {
                if (!TryGetInvokeArguments(methodInfo.GetParameters(), args, argsOffset, out invokeArgs, out var errorMessage))
                {
                    await SetFailAsync(errorMessage + " args: " + string.Join(" ", args));
                    return;
                }
            }
            catch (Exception ex)
            {
                await SetFailAsync("Fail to match method parameter on " + type.Name + "." + methodInfo.Name + ". args: " + string.Join(" ", args), ex);
                return;
            }

            var ctx = new ConsoleAppContext(args, DateTime.UtcNow, cancellationToken, logger, methodInfo, provider);
            try
            {
                instance = provider.GetService(type);
                typeof(ConsoleAppBase).GetProperty(nameof(ConsoleAppBase.Context)).SetValue(instance, ctx);
            }
            catch (Exception ex)
            {
                await SetFailAsync("Fail to create ConsoleAppBase instance. Type:" + type.FullName, ex);
                return;
            }

            try
            {
                var invoker = new WithFilterInvoker(methodInfo, instance, invokeArgs, provider, options.GlobalFilters ?? Array.Empty<ConsoleAppFilter>(), ctx);
                var result = await invoker.InvokeAsync();
                if (result != null)
                {
                    Environment.ExitCode = result.Value;
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException operationCanceledException && operationCanceledException.CancellationToken == cancellationToken)
                {
                    // NOTE: Do nothing if the exception has thrown by the CancellationToken of ConsoleAppEngine.
                    // If the user code throws OperationCanceledException, ConsoleAppEngine should not handle that.
                    return;
                }

                if (ex is TargetInvocationException tex)
                {
                    await SetFailAsync("Fail in console app running on " + type.Name + "." + methodInfo.Name, tex.InnerException);
                    return;
                }
                else
                {
                    await SetFailAsync("Fail in console app running on " + type.Name + "." + methodInfo.Name, ex);
                    return;
                }
            }

            logger.LogTrace("ConsoleAppEngine.Run Complete Successfully");
        }

        ValueTask SetFailAsync(string message)
        {
            Environment.ExitCode = 1;
            logger.LogError(message);
            return default;
        }

        ValueTask SetFailAsync(string message, Exception ex)
        {
            Environment.ExitCode = 1;
            logger.LogError(ex, message);
            return default;
        }

        bool TryGetInvokeArguments(ParameterInfo[] parameters, string?[] args, int argsOffset, out object[] invokeArgs, out string? errorMessage)
        {
            var jsonOption = options.JsonSerializerOptions;

            // Collect option types for parsing command-line arguments.
            var optionTypeByOptionName = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < parameters.Length; i++)
            {
                var item = parameters[i];
                var option = item.GetCustomAttribute<OptionAttribute>();

                optionTypeByOptionName[(isStrict ? "--" : "") + item.Name] = item.ParameterType;
                if (!string.IsNullOrWhiteSpace(option?.ShortName))
                {
                    optionTypeByOptionName[(isStrict ? "-" : "") + option!.ShortName!] = item.ParameterType;
                }
            }

            var (argumentDictionary, optionByIndex) = ParseArgument(args, argsOffset, optionTypeByOptionName, isStrict);
            invokeArgs = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var item = parameters[i];
                var option = item.GetCustomAttribute<OptionAttribute>();
                if (!string.IsNullOrWhiteSpace(option?.ShortName) && char.IsDigit(option!.ShortName, 0)) throw new InvalidOperationException($"Option '{item.Name}' has a short name, but the short name must start with A-Z or a-z.");

                var value = default(OptionParameter);

                // Indexed arguments (e.g. [Option(0)])
                if (option != null && option.Index != -1)
                {
                    if (optionByIndex.Count <= option.Index)
                    {
                        if (!item.HasDefaultValue)
                        {
                            throw new InvalidOperationException($"Required argument {option.Index} was not found in specified arguments.");
                        }
                    }
                    else
                    {
                        value = optionByIndex[option.Index];
                    }
                }

                // Keyed options (e.g. -foo -bar )
                var longName = (isStrict) ? ("--" + item.Name) : item.Name;
                var shortName = (isStrict) ? ("-" + option?.ShortName?.TrimStart('-')) : option?.ShortName?.TrimStart('-');

                if (value.Value != null || argumentDictionary.TryGetValue(longName, out value) || argumentDictionary.TryGetValue(shortName ?? "", out value))
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
                                var elemType = UnwrapCollectionElementType(parameters[i].ParameterType);
                                if (elemType == typeof(string))
                                {
                                    if (!(v.StartsWith("\"") && v.EndsWith("\"")))
                                    {
                                        v = "[" + string.Join(",", v.Split(' ', ',').Select(x => "\"" + x + "\"")) + "]";
                                    }
                                    else
                                    {
                                        v = "[" + v + "]";
                                    }
                                }
                                else
                                {
                                    v = "[" + string.Join(",", v.Trim('\'', '\"').Split(' ', ',')) + "]";
                                }
                            }
                            try
                            {
                                invokeArgs[i] = JsonSerializer.Deserialize(v, parameters[i].ParameterType, jsonOption);
                                continue;
                            }
                            catch
                            {
                                errorMessage = "Parameter \"" + item.Name + "\"" + " fail on JSON deserialize, please check type or JSON escape or add double-quotation.";
                                return false;
                            }
                        }
                        else
                        {
                            try
                            {
                                invokeArgs[i] = JsonSerializer.Deserialize(value.Value, parameters[i].ParameterType, jsonOption);
                                continue;
                            }
                            catch
                            {
                                errorMessage = "Parameter \"" + item.Name + "\"" + " fail on JSON deserialize, please check type or JSON escape or add double-quotation.";
                                return false;
                            }
                        }
                    }
                }

                if (item.HasDefaultValue)
                {
                    invokeArgs[i] = item.DefaultValue;
                }
                else if (item.ParameterType == typeof(bool))
				{
                    // bool without default value should be considered that it has implicit default value of false.
                    invokeArgs[i] = false;
				}
                else
                {
                    var name = item.Name;
                    if (option?.ShortName != null)
                    {
                        name = item.Name + "(" + "-" + option.ShortName + ")";
                    }
                    errorMessage = "Required parameter \"" + name + "\"" + " not found in argument.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        static Type? UnwrapCollectionElementType(Type collectionType)
        {
            if (collectionType.IsArray)
            {
                return collectionType.GetElementType();
            }

            foreach (var i in collectionType.GetInterfaces())
            {
                if (i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                {
                    return i.GetGenericArguments()[0];
                }
            }

            return null;
        }

        static (ReadOnlyDictionary<string, OptionParameter> OptionByKey, IReadOnlyList<OptionParameter> OptionByIndex) ParseArgument(string?[] args, int argsOffset, IReadOnlyDictionary<string, Type> optionTypeByName, bool isStrict)
        {
            var dict = new Dictionary<string, OptionParameter>(args.Length, StringComparer.OrdinalIgnoreCase);
            var options = new List<OptionParameter>();
            for (int i = argsOffset; i < args.Length;)
            {
                var arg = args[i++];
                if (arg is null || !arg.StartsWith("-"))
                {
                    options.Add(new OptionParameter() { Value = arg });
                    continue; // not key
                }

                var key = (isStrict) ? arg : arg.TrimStart('-');

                if (optionTypeByName.TryGetValue(key, out var optionType))
                {
                    if (optionType == typeof(bool))
                    {
                        var boolValue = true;
                        if (i < args.Length)
                        {
                            var isTrue = args[i]?.Equals("true", StringComparison.OrdinalIgnoreCase);
                            var isFalse = args[i]?.Equals("false", StringComparison.OrdinalIgnoreCase);
                            if (isTrue != null && isTrue.Value)
                            {
                                boolValue = true;
                            }
                            else if (isFalse != null && isFalse.Value)
                            {
                                boolValue = false;
                            }
                        }

                        dict.Add(key, new OptionParameter { BooleanSwitch = boolValue });
                    }
                    else
                    {
                        var value = args[i];
                        dict.Add(key, new OptionParameter { Value = value });
                        i++;
                    }
                }
                else
                {
                    // not key
                    options.Add(new OptionParameter() { Value = arg });
                }
            }

            return (new ReadOnlyDictionary<string, OptionParameter>(dict), options);
        }

        struct OptionParameter
        {
            public string? Value;
            public bool BooleanSwitch;
        }

        class CustomSorter : IComparer<MethodInfo>
        {
            public int Compare(MethodInfo x, MethodInfo y)
            {
                if (x.Name == y.Name)
                {
                    return 0;
                }

                var xc = x.GetCustomAttribute<CommandAttribute>();
                var yc = y.GetCustomAttribute<CommandAttribute>();

                if (xc != null)
                {
                    return 1;
                }
                if (yc != null)
                {
                    return -1;
                }

                return x.Name.CompareTo(y.Name);
            }
        }
    }
}