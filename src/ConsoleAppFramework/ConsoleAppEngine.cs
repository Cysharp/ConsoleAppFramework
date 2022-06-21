using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleAppFramework
{
    internal class ConsoleAppEngine
    {
        readonly ILogger<ConsoleApp> logger;
        readonly IServiceProvider provider;
        readonly CancellationTokenSource cancellationTokenSource;
        readonly ConsoleAppOptions options;
        readonly IServiceProviderIsService isService;
        readonly IParamsValidator paramsValidator;
        readonly bool isStrict;

        public ConsoleAppEngine(ILogger<ConsoleApp> logger,
            IServiceProvider provider,
            ConsoleAppOptions options,
            IServiceProviderIsService isService,
            IParamsValidator paramsValidator,
            CancellationTokenSource cancellationTokenSource)
        {
            this.logger = logger;
            this.provider = provider;
            this.paramsValidator = paramsValidator;
            this.cancellationTokenSource = cancellationTokenSource;
            this.options = options;
            this.isService = isService;
            this.isStrict = options.StrictOption;
        }

        public async Task RunAsync()
        {
            logger.LogTrace("ConsoleAppEngine.Run Start");

            var args = options.CommandLineArguments;

            if (!options.CommandDescriptors.TryGetDescriptor(args, out var commandDescriptor, out var offset))
            {
                if (args.Length == 0)
                {
                    if (options.CommandDescriptors.TryGetHelpMethod(out commandDescriptor))
                    {
                        goto RUN;
                    }
                }

                // TryGet Single help or Version
                if (args.Length == 1)
                {
                    switch (args[0].Trim('-'))
                    {
                        case "help":
                            if (options.CommandDescriptors.TryGetHelpMethod(out commandDescriptor))
                            {
                                goto RUN;
                            }
                            break;
                        case "version":
                            if (options.CommandDescriptors.TryGetVersionMethod(out commandDescriptor))
                            {
                                goto RUN;
                            }
                            break;
                        default:
                            break;
                    }
                }

                // TryGet SubCommands Help
                if (args.Length >= 2 && args[1].Trim('-') == "help")
                {
                    var subCommands = options.CommandDescriptors.GetSubCommands(args[0]);
                    if (subCommands.Length != 0)
                    {
                        var msg = new CommandHelpBuilder(() => args[0], isService, options).BuildHelpMessage(null, subCommands, shortCommandName: true);
                        Console.WriteLine(msg);
                        return;
                    }
                }

                await SetFailAsync("Command not found. args: " + string.Join(" ", args));
                return;
            }

            // foo --help
            // foo bar --help
            if (args.Skip(offset).FirstOrDefault()?.Trim('-') == "help")
            {
                var msg = new CommandHelpBuilder(() => commandDescriptor.GetCommandName(options), isService, options).BuildHelpMessage(commandDescriptor);
                Console.WriteLine(msg);
                return;
            }

            // check can invoke help
            if (commandDescriptor.CommandType == CommandType.DefaultCommand && args.Length == 0)
            {
                var p = commandDescriptor.MethodInfo.GetParameters();
                if (p.Any(x => !(x.ParameterType == typeof(ConsoleAppContext) || isService.IsService(x.ParameterType) || x.HasDefaultValue)))
                {
                    options.CommandDescriptors.TryGetHelpMethod(out commandDescriptor);
                }
            }

        RUN:
            await RunCore(commandDescriptor!.MethodInfo!.DeclaringType!, commandDescriptor.MethodInfo, commandDescriptor.Instance, args, offset);
        }

        // Try to invoke method.
        async Task RunCore(Type type, MethodInfo methodInfo, object? instance, string?[] args, int argsOffset)
        {
            object?[] invokeArgs;
            ParameterInfo[] originalParameters = methodInfo.GetParameters();
            var isService = provider.GetService<IServiceProviderIsService>();
            try
            {
                var parameters = originalParameters;
                if (isService != null)
                {
                    parameters = parameters.Where(x => !(x.ParameterType == typeof(ConsoleAppContext) || isService.IsService(x.ParameterType))).ToArray();
                }

                if (!TryGetInvokeArguments(parameters, args, argsOffset, out invokeArgs, out var errorMessage))
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

            var ctx = new ConsoleAppContext(args, DateTime.UtcNow, cancellationTokenSource, logger, methodInfo, provider);

            // re:create invokeArgs, merge with DI parameter.
            if (invokeArgs.Length != originalParameters.Length)
            {
                var newInvokeArgs = new object?[originalParameters.Length];
                var invokeArgsIndex = 0;
                for (int i = 0; i < originalParameters.Length; i++)
                {
                    var p = originalParameters[i].ParameterType;
                    if (p == typeof(ConsoleAppContext))
                    {
                        newInvokeArgs[i] = ctx;
                    }
                    else if (isService!.IsService(p))
                    {
                        try
                        {
                            newInvokeArgs[i] = provider.GetService(p);
                        }
                        catch (Exception ex)
                        {
                            await SetFailAsync("Fail to get service parameter. ParameterType:" + p.FullName, ex);
                            return;
                        }
                    }
                    else
                    {
                        newInvokeArgs[i] = invokeArgs[invokeArgsIndex++];
                    }
                }
                invokeArgs = newInvokeArgs;
            }

            var validationResult = paramsValidator.ValidateParameters(originalParameters.Zip(invokeArgs));
            if (validationResult != ValidationResult.Success)
            {
                await SetFailAsync(validationResult!.ErrorMessage!);
                return;
            }

            try
            {
                if (instance == null && !type.IsAbstract && !methodInfo.IsStatic)
                {
                    instance = ActivatorUtilities.CreateInstance(provider, type);
                    typeof(ConsoleAppBase).GetProperty(nameof(ConsoleAppBase.Context))!.SetValue(instance, ctx);
                }

            }
            catch (Exception ex)
            {
                await SetFailAsync("Fail to create ConsoleAppBase instance. Type:" + type.FullName, ex);
                return;
            }

            try
            {
                var invoker = new WithFilterInvoker(methodInfo, instance, invokeArgs, provider, options.GlobalFilters ?? Array.Empty<ConsoleAppFilter>(), ctx);
                try
                {
                    var result = await invoker.InvokeAsync();
                    if (result != null)
                    {
                        Environment.ExitCode = result.Value;
                    }
                }
                finally
                {
                    if (instance is IAsyncDisposable ad)
                    {
                        await ad.DisposeAsync();
                    }
                    else if (instance is IDisposable d)
                    {
                        d.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException tex)
                {
                    ex = tex.InnerException ?? tex;
                }

                if (ex is OperationCanceledException operationCanceledException && operationCanceledException.CancellationToken == cancellationTokenSource.Token)
                {
                    // NOTE: Do nothing if the exception has thrown by the CancellationToken of ConsoleAppEngine.
                    // If the user code throws OperationCanceledException, ConsoleAppEngine should not handle that.
                    return;
                }

                await SetFailAsync("Fail in application running on " + type.Name + "." + methodInfo.Name, ex);
                return;
            }

            logger.LogTrace("ConsoleAppEngine.Run Complete Successfully");
        }

        ValueTask SetFailAsync(string message)
        {
            Environment.ExitCode = 1;
            logger.LogError(message);
            return default;
        }

        ValueTask SetFailAsync(string message, Exception? ex)
        {
            Environment.ExitCode = 1;
            logger.LogError(ex, message);
            return default;
        }

        bool TryGetInvokeArguments(ParameterInfo[] parameters, string?[] args, int argsOffset, out object?[] invokeArgs, out string? errorMessage)
        {
            try
            {
                var jsonOption = options.JsonSerializerOptions;

                // Collect option types for parsing command-line arguments.
                var optionTypeByOptionName = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < parameters.Length; i++)
                {
                    var item = parameters[i];
                    var option = item.GetCustomAttribute<OptionAttribute>();

                    optionTypeByOptionName[(isStrict ? "--" : "") + options.NameConverter(item.Name!)] = item.ParameterType;
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
                    var itemName = options.NameConverter(item.Name!);
                    var option = item.GetCustomAttribute<OptionAttribute>();
                    if (!string.IsNullOrWhiteSpace(option?.ShortName) && char.IsDigit(option!.ShortName, 0)) throw new InvalidOperationException($"Option '{itemName}' has a short name, but the short name must start with A-Z or a-z.");

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
                    var longName = (isStrict) ? ("--" + itemName) : itemName;
                    var shortName = (isStrict) ? ("-" + option?.ShortName?.TrimStart('-')) : option?.ShortName?.TrimStart('-');

                    if (value.Value != null || argumentDictionary.TryGetValue(longName!, out value) || argumentDictionary.TryGetValue(shortName ?? "", out value))
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
                                    errorMessage = "Parameter \"" + itemName + "\"" + " fail on Enum parsing.";
                                    return false;
                                }
                            }
                            else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(parameters[i].ParameterType) && !typeof(System.Collections.IDictionary).IsAssignableFrom(parameters[i].ParameterType))
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
                                    errorMessage = "Parameter \"" + itemName + "\"" + " fail on JSON deserialize, please check type or JSON escape or add double-quotation.";
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
                                    errorMessage = "Parameter \"" + itemName + "\"" + " fail on JSON deserialize, please check type or JSON escape or add double-quotation.";
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
                        var name = itemName;
                        if (option?.ShortName != null)
                        {
                            name = itemName + "(" + "-" + option.ShortName + ")";
                        }
                        errorMessage = "Required parameter \"" + name + "\"" + " not found in argument.";
                        return false;
                    }
                }

                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                invokeArgs = default!;
                errorMessage = ex.Message;
                return false;
            }
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
    }
}