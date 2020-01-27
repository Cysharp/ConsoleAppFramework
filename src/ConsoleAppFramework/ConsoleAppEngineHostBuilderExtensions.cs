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
        public static IHostBuilder UseConsoleAppFramework(this IHostBuilder hostBuilder, string[] args, IConsoleAppInterceptor? interceptor = null)
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

            // () or -help
            if (args.Length == 0 || (args.Length == 1 && TrimEquals(args[0], HelpCommand)))
            {
                ShowMethodList();
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
                    var (t, mi) = GetTypeFromAssemblies(args[methodIndex], null);
                    if (mi != null)
                    {
                        Console.Write(new CommandHelpBuilder().BuildHelpMessage(mi, showCommandName: true));
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
                (type, methodInfo) = GetTypeFromAssemblies(args[0], null);
            }

            hostBuilder = hostBuilder
                .ConfigureServices(services =>
                {
                    services.AddOptions<ConsoleLifetimeOptions>().Configure(x => x.SuppressStatusMessages = true);
                    services.AddSingleton<string[]>(args);
                    services.AddSingleton<IHostedService, ConsoleAppEngineService>();
                    services.AddSingleton<IConsoleAppInterceptor>(interceptor ?? NullConsoleAppInterceptor.Default);
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
                });

            return hostBuilder.UseConsoleLifetime();
        }

        /// <summary>
        /// Run multiple ConsoleApp that are searched from all assemblies.
        /// </summary>
        public static Task RunConsoleAppFrameworkAsync(this IHostBuilder hostBuilder, string[] args, IConsoleAppInterceptor? interceptor = null)
        {
            return UseConsoleAppFramework(hostBuilder, args, interceptor).Build().RunAsync();
        }

        /// <summary>
        /// Setup a single ConsoleApp type that is targeted by type argument.
        /// </summary>
        public static IHostBuilder UseConsoleAppFramework<T>(this IHostBuilder hostBuilder, string[] args, IConsoleAppInterceptor? interceptor = null)
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
                        Console.Write(new CommandHelpBuilder().BuildHelpMessage(methods, null));
                        ConfigureEmptyService();
                        return hostBuilder;
                    }
                    else
                    {
                        // override default Help
                        args = new string[] { "help" };
                    }
                }
            }

            if (!hasHelp && args.Length == 1 && TrimEquals(args[0], HelpCommand))
            {
                Console.Write(new CommandHelpBuilder().BuildHelpMessage(methods, defaultMethod));
                ConfigureEmptyService();
                return hostBuilder;
            }

            if (args.Length == 1 && TrimEquals(args[0], VersionCommand))
            {
                ShowVersion();
                ConfigureEmptyService();
                return hostBuilder;
            }

            if (args.Length == 2 && methods.Length != 1)
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
                    var (_, mi) = GetTypeFromAssemblies(args[methodIndex], typeof(T));
                    if (mi != null)
                    {
                        Console.Write(new CommandHelpBuilder().BuildHelpMessage(mi, showCommandName: true));
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
                services.AddSingleton<IConsoleAppInterceptor>(interceptor ?? NullConsoleAppInterceptor.Default);
                services.AddTransient<T>();
            });

            return hostBuilder.UseConsoleLifetime();
        }

        /// <summary>
        /// Run a single ConsoleApp type that is targeted by type argument.
        /// </summary>
        public static Task RunConsoleAppFrameworkAsync<T>(this IHostBuilder hostBuilder, string[] args, IConsoleAppInterceptor? interceptor = null)
            where T : ConsoleAppBase
        {
            return UseConsoleAppFramework<T>(hostBuilder, args, interceptor).Build().RunAsync();
        }

        static bool TrimEquals(string arg, string command)
        {
            return arg.Trim('-').Equals(command, StringComparison.OrdinalIgnoreCase);
        }

        static void ShowVersion()
        {
            var asm = Assembly.GetEntryAssembly();
            var version = "1.0.0";
            var infoVersion = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (infoVersion != null)
            {
                version = infoVersion.InformationalVersion;
            }
            else
            {
                var asmVersion = asm.GetCustomAttribute<AssemblyVersionAttribute>();
                if (asmVersion != null)
                {
                    version = asmVersion.Version;
                }
            }
            Console.WriteLine(version);
        }

        static void ShowMethodList()
        {
            Console.Write(new CommandHelpBuilder().BuildHelpMessage(GetConsoleAppTypes()));
        }

        static List<Type> GetConsoleAppTypes()
        {
            List<Type> consoleAppBaseTypes = new List<Type>();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.FullName.StartsWith("System") || asm.FullName.StartsWith("Microsoft.Extensions")) continue;

                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                foreach (var item in types)
                {
                    if (typeof(ConsoleAppBase).IsAssignableFrom(item) && item != typeof(ConsoleAppBase))
                    {
                        consoleAppBaseTypes.Add(item);
                    }
                }
            }

            return consoleAppBaseTypes;
        }

        static (Type?, MethodInfo?) GetTypeFromAssemblies(string arg0, Type? defaultBaseType)
        {
            var consoleAppBaseTypes = (defaultBaseType == null)
                ? GetConsoleAppTypes()
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
                foreach (var (method, cmdattr) in baseType.GetMethods().
                    Select(m => (MethodInfo: m, Attr: m.GetCustomAttribute<CommandAttribute>())).Where(x => x.Attr != null))
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
                        foundMethod = baseType.GetMethod(split[1]);
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