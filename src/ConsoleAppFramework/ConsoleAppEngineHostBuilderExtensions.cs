using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ConsoleAppFramework
{
    public static class ConsoleAppEngineHostBuilderExtensions
    {
        const string HelpCommand = "help";
        const string VersionCommand = "version";

        /// <summary>
        /// Setup multiple ConsoleApp that are searched from all assemblies.
        /// </summary>
        public static IHostBuilder UseConsoleAppFramework(this IHostBuilder hostBuilder, string[] args, ConsoleAppOptions? options = null, Assembly[]? searchAssemblies = null)
        {
            IHostBuilder ConfigureEmptyService()
            {
                hostBuilder.ConfigureServices(services =>
                {
                    services.AddOptions<ConsoleLifetimeOptions>().Configure(x => x.SuppressStatusMessages = true);
                    services.AddSingleton<IHostedService, EmptyHostedService>();
                });
                return hostBuilder;
            }

            searchAssemblies ??= AppDomain.CurrentDomain.GetAssemblies();
            if (options == null) options = new ConsoleAppOptions();

            // () or -help
            if (args.Length == 0 || (args.Length == 1 && TrimEquals(args[0], HelpCommand)))
            {
                ShowMethodList(searchAssemblies, options);
                ConfigureEmptyService();
                return hostBuilder;
            }

            // -version
            if (args.Length == 1 && TrimEquals(args[0], VersionCommand))
            {
                ShowVersion();
                ConfigureEmptyService();
                return hostBuilder;
            }

            // backward compatibility, logic use Class.Method
            if (!args[0].Contains(".") && args.Length >= 2)
            {
                var newArgs = new string[args.Length - 1];
                newArgs[0] = args[0] + "." + args[1];
                Array.Copy(args, 2, newArgs, 1, newArgs.Length - 1);
                args = newArgs;
            }

            if (args.Length == 2)
            {
                int methodIndex = -1;

                // help command
                if (TrimEquals(args[0], HelpCommand))
                {
                    methodIndex = 1;
                }
                // command -help
                else if (TrimEquals(args[1], HelpCommand))
                {
                    methodIndex = 0;
                }

                if (methodIndex != -1)
                {
                    var (t, mi) = GetTypeFromAssemblies(args[methodIndex], null, searchAssemblies);
                    if (mi != null)
                    {
                        Console.Write(new CommandHelpBuilder(null, options.StrictOption, options.ShowDefaultCommand).BuildHelpMessage(mi, showCommandName: true, true));
                    }
                    else
                    {
                        Console.Error.WriteLine("Method not found, please check \"help\" command.");
                    }
                    ConfigureEmptyService();
                    return hostBuilder;
                }
            }

            Type? type = null;
            MethodInfo? methodInfo = null;
            if (args.Length >= 1)
            {
                (type, methodInfo) = GetTypeFromAssemblies(args[0], null, searchAssemblies);
            }

            hostBuilder = hostBuilder
                .ConfigureServices(services =>
                {
                    services.AddOptions<ConsoleLifetimeOptions>().Configure(x => x.SuppressStatusMessages = true);
                    services.AddSingleton<string[]>(args);
                    services.AddSingleton<IHostedService, ConsoleAppEngineService>();
                    services.AddSingleton<ConsoleAppOptions>(options ?? new ConsoleAppOptions());
                    if (type != null)
                    {
                        services.AddSingleton<Type>(type);
                        services.AddTransient(type);
                    }
                    else
                    {
                        services.AddSingleton<Type>(typeof(void));
                    }

                    if (methodInfo != null)
                    {
                        services.AddSingleton<MethodInfo>(methodInfo);
                    }

                    foreach (var item in CollectFilterType(type, methodInfo))
                    {
                        services.AddTransient(item);
                    }
                });

            return hostBuilder.UseConsoleLifetime();
        }

        static IEnumerable<Type> CollectFilterType(Type? type, MethodInfo? methodInfo)
        {
            var set = new HashSet<Type>();
            IEnumerable<ConsoleAppFilterAttribute> filters = Array.Empty<ConsoleAppFilterAttribute>();
            if (type != null)
            {
                var filtersA = type.GetCustomAttributes<ConsoleAppFilterAttribute>(true);
                var filtersB = type.GetMethods(BindingFlags.Public | BindingFlags.Instance).SelectMany(x => x.GetCustomAttributes<ConsoleAppFilterAttribute>(true));
                filters = filtersA.Concat(filtersB);
            }

            if (methodInfo != null)
            {
                filters = filters.Concat(methodInfo.GetCustomAttributes<ConsoleAppFilterAttribute>(true));
            }

            foreach (var item in filters)
            {
                set.Add(item.Type);
            }

            return set;
        }

        /// <summary>
        /// Run multiple ConsoleApp that are searched from all assemblies.
        /// </summary>
        public static Task RunConsoleAppFrameworkAsync(this IHostBuilder hostBuilder, string[] args, ConsoleAppOptions? options = null, Assembly[]? searchAssemblies = null)
        {
            return UseConsoleAppFramework(hostBuilder, args, options, searchAssemblies).Build().RunAsync();
        }

        /// <summary>
        /// Setup a single ConsoleApp type that is targeted by type argument.
        /// </summary>
        public static IHostBuilder UseConsoleAppFramework<T>(this IHostBuilder hostBuilder, string[] args, ConsoleAppOptions? options = null, Assembly[]? searchAssemblies = null)
            where T : ConsoleAppBase
        {
            IHostBuilder ConfigureEmptyService()
            {
                hostBuilder.ConfigureServices(services =>
                {
                    services.AddOptions<ConsoleLifetimeOptions>().Configure(x => x.SuppressStatusMessages = true);
                    services.AddSingleton<IHostedService, EmptyHostedService>();
                });
                return hostBuilder;
            }

            searchAssemblies ??= AppDomain.CurrentDomain.GetAssemblies();
            if (options == null) options = new ConsoleAppOptions();

            var methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var defaultMethod = methods.FirstOrDefault(x => x.GetCustomAttribute<CommandAttribute>() == null);
            var hasHelp = methods.Any(x => x.GetCustomAttribute<CommandAttribute>()?.EqualsAny(HelpCommand) ?? false);
            var hasVersion = methods.Any(x => x.GetCustomAttribute<CommandAttribute>()?.EqualsAny(VersionCommand) ?? false);

            if (args.Length == 0)
            {
                if (defaultMethod == null || (defaultMethod.GetParameters().Length != 0 && !defaultMethod.GetParameters().All(x => x.HasDefaultValue)))
                {
                    if (!hasHelp)
                    {
                        Console.Write(new CommandHelpBuilder(null, options.StrictOption, options.ShowDefaultCommand).BuildHelpMessage(methods, defaultMethod));
                        ConfigureEmptyService();
                        return hostBuilder;
                    }
                    else
                    {
                        // override default Help
                        args = new string[] { "--help" };
                    }
                }
            }

            if (!hasHelp && args.Length == 1 && OptionEquals(args[0], HelpCommand))
            {
                Console.Write(new CommandHelpBuilder(null, options.StrictOption, options.ShowDefaultCommand).BuildHelpMessage(methods, defaultMethod));
                ConfigureEmptyService();
                return hostBuilder;
            }

            if (args.Length == 1 && OptionEquals(args[0], VersionCommand))
            {
                ShowVersion();
                ConfigureEmptyService();
                return hostBuilder;
            }

            if (args.Length == 2 && methods.Length > 0 && defaultMethod == null)
            {
                int methodIndex = -1;

                // help command
                if (TrimEquals(args[0], HelpCommand))
                {
                    methodIndex = 1;
                }
                // command -help
                else if (OptionEquals(args[1], HelpCommand))
                {
                    methodIndex = 0;
                }

                if (methodIndex != -1)
                {
                    var (_, mi) = GetTypeFromAssemblies(args[methodIndex], typeof(T), searchAssemblies);
                    if (mi != null)
                    {
                        Console.Write(new CommandHelpBuilder(null, options.StrictOption, options.ShowDefaultCommand).BuildHelpMessage(mi, showCommandName: true, fromMultiCommand: false));
                        ConfigureEmptyService();
                        return hostBuilder;
                    }
                }
            }

            hostBuilder = hostBuilder.ConfigureServices(services =>
            {
                services.AddOptions<ConsoleLifetimeOptions>().Configure(x => x.SuppressStatusMessages = true);
                services.AddSingleton<string[]>(args);
                services.AddSingleton<Type>(typeof(T));
                services.AddSingleton<IHostedService, ConsoleAppEngineService>();
                services.AddSingleton<ConsoleAppOptions>(options ?? new ConsoleAppOptions());
                services.AddTransient<T>();

                foreach (var item in CollectFilterType(typeof(T), null))
                {
                    services.AddTransient(item);
                }
            });

            return hostBuilder.UseConsoleLifetime();
        }

        /// <summary>
        /// Run a single ConsoleApp type that is targeted by type argument.
        /// </summary>
        public static Task RunConsoleAppFrameworkAsync<T>(this IHostBuilder hostBuilder, string[] args, ConsoleAppOptions? options = null)
            where T : ConsoleAppBase
        {
            return UseConsoleAppFramework<T>(hostBuilder, args, options).Build().RunAsync();
        }

        static bool TrimEquals(string arg, string command)
        {
            return arg.Trim('-').Equals(command, StringComparison.OrdinalIgnoreCase);
        }

        static bool OptionEquals(string arg, string command)
        {
            // v3, same as TrimEquals.
            // return arg.StartsWith("-") && arg.Trim('-').Equals(command, StringComparison.OrdinalIgnoreCase);
            return arg.Trim('-').Equals(command, StringComparison.OrdinalIgnoreCase);
        }

        static void ShowVersion()
        {
            var asm = Assembly.GetEntryAssembly();
            var version = "1.0.0";
            var infoVersion = asm!.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (infoVersion != null)
            {
                version = infoVersion.InformationalVersion;
            }
            else
            {
                var asmVersion = asm!.GetCustomAttribute<AssemblyVersionAttribute>();
                if (asmVersion != null)
                {
                    version = asmVersion.Version;
                }
            }
            Console.WriteLine(version);
        }

        static void ShowMethodList(Assembly[] searchAssemblies, ConsoleAppOptions options)
        {
            Console.Write(new CommandHelpBuilder(null, options.StrictOption, options.ShowDefaultCommand).BuildHelpMessage(GetConsoleAppTypes(searchAssemblies)));
        }

        static List<Type> GetConsoleAppTypes(Assembly[] searchAssemblies)
        {
            List<Type> consoleAppBaseTypes = new List<Type>();

            foreach (var asm in searchAssemblies)
            {
                if (asm.FullName!.StartsWith("System") || asm.FullName.StartsWith("Microsoft.Extensions")) continue;

                Type?[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                foreach (var item in types.Where(x => x != null))
                {
                    if (typeof(ConsoleAppBase).IsAssignableFrom(item) && item != typeof(ConsoleAppBase))
                    {
                        consoleAppBaseTypes.Add(item!);
                    }
                }
            }

            return consoleAppBaseTypes;
        }

        static (Type?, MethodInfo?) GetTypeFromAssemblies(string arg0, Type? defaultBaseType, Assembly[] searchAssemblies)
        {
            var consoleAppBaseTypes = (defaultBaseType == null)
                ? GetConsoleAppTypes(searchAssemblies)
                : new List<Type> { defaultBaseType };

            if (consoleAppBaseTypes == null)
            {
                return (null, null);
            }

            var split = arg0.Split('.');
            Type? foundType = null;
            MethodInfo? foundMethod = null;
            foreach (var baseType in consoleAppBaseTypes)
            {
                bool isFound = false;
                foreach (var (method, cmdattr) in baseType.GetMethods().Select(m => (MethodInfo: m, Attr: m.GetCustomAttribute<CommandAttribute>())).Where(x => x.Attr != null))
                {
                    if (cmdattr.CommandNames.Any(x => TrimEquals(arg0, x)))
                    {
                        if (foundType != null && foundMethod != null)
                        {
                            throw new InvalidOperationException($"Duplicate ConsoleApp Command name is not allowed, {foundType.FullName}.{foundMethod.Name} and {baseType.FullName}.{method.Name}");
                        }
                        foundType = baseType;
                        foundMethod = method;
                        isFound = true;
                    }
                }
                if (!isFound && split.Length == 2)
                {
                    if (baseType.Name.Equals(split[0], StringComparison.OrdinalIgnoreCase))
                    {
                        if (foundType != null)
                        {
                            throw new InvalidOperationException("Duplicate ConsoleApp TypeName is not allowed, " + foundType.FullName + " and " + baseType.FullName);
                        }
                        foundType = baseType;

                        foreach (var m in baseType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.IgnoreCase))
                        {
                            var commandAliases = m.GetCustomAttribute<CommandAttribute>()?.CommandNames ?? new[] { m.Name };
                            foreach (var ca in commandAliases)
                            {
                                if (ca.Equals(split[1], StringComparison.OrdinalIgnoreCase))
                                {
                                    foundMethod = m;
                                    break;
                                }
                            }
                            if (foundMethod != null) break;
                        }
                    }
                }
            }
            if (foundType != null && foundMethod != null)
            {
                return (foundType, foundMethod);
            }
            return (null, null);

        }
    }
}