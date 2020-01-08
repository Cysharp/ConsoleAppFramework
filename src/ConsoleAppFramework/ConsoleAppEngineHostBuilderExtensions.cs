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
        const string ListCommand = "list";
        const string HelpCommand = "help";

        public static IHostBuilder UseConsoleAppEngine(this IHostBuilder hostBuilder, string[] args, IConsoleAppInterceptor? interceptor = null)
        {
            if (args.Length == 0 || (args.Length == 1 && args[0].Equals(ListCommand, StringComparison.OrdinalIgnoreCase)))
            {
                ShowMethodList();
                hostBuilder.ConfigureServices(services =>
                {
                    services.AddOptions<ConsoleLifetimeOptions>().Configure(x => x.SuppressStatusMessages = true);
                    services.AddSingleton<IHostedService, EmptyHostedService>();
                });
                return hostBuilder;
            }
            if (args.Length == 2 && args[0].Equals(HelpCommand, StringComparison.OrdinalIgnoreCase))
            {
                var (t, mi) = GetTypeFromAssemblies(args[1]);
                if (mi != null)
                {
                    Console.Write(new CommandHelpBuilder().BuildHelpMessage(mi, showCommandName: true));
                }
                else
                {
                    Console.Error.WriteLine("Method not found , please check \"list\" command.");
                }
                hostBuilder.ConfigureServices(services =>
                {
                    services.AddOptions<ConsoleLifetimeOptions>().Configure(x => x.SuppressStatusMessages = true);
                    services.AddSingleton<IHostedService, EmptyHostedService>();
                });
                return hostBuilder;
            }

            Type? type = null;
            MethodInfo? methodInfo = null;
            if (args.Length >= 1)
            {
                (type, methodInfo) = GetTypeFromAssemblies(args[0]);
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

        public static Task RunConsoleAppEngineAsync(this IHostBuilder hostBuilder, string[] args, IConsoleAppInterceptor? interceptor = null)
        {
            return UseConsoleAppEngine(hostBuilder, args, interceptor).Build().RunAsync();
        }

        public static IHostBuilder UseConsoleAppEngine<T>(this IHostBuilder hostBuilder, string[] args, IConsoleAppInterceptor? interceptor = null)
            where T : ConsoleAppBase
        {
            var method = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var defaultMethod = method.FirstOrDefault(x => x.GetCustomAttribute<CommandAttribute>() == null);
            var hasList = method.Any(x => x.GetCustomAttribute<CommandAttribute>()?.EqualsAny(ListCommand) ?? false);
            var hasHelp = method.Any(x => x.GetCustomAttribute<CommandAttribute>()?.EqualsAny(HelpCommand) ?? false);

            if (args.Length == 0)
            {
                if (defaultMethod == null || (defaultMethod.GetParameters().Length != 0 && !defaultMethod.GetParameters().All(x => x.HasDefaultValue)))
                {
                    if (!hasHelp)
                    {
                        Console.Write(new CommandHelpBuilder().BuildHelpMessage(method, defaultMethod));

                        hostBuilder.ConfigureServices(services =>
                        {
                            services.AddOptions<ConsoleLifetimeOptions>().Configure(x => x.SuppressStatusMessages = true);
                            services.AddSingleton<IHostedService, EmptyHostedService>();
                        });
                        return hostBuilder;
                    }
                    else
                    {
                        // override default Help
                        args = new string[] { "help" };
                    }
                }
            }

            if (!hasList && args.Length == 1 && args[0].Equals(ListCommand, StringComparison.OrdinalIgnoreCase))
            {
                ShowMethodList();
                hostBuilder.ConfigureServices(services =>
                {
                    services.AddOptions<ConsoleLifetimeOptions>().Configure(x => x.SuppressStatusMessages = true);
                    services.AddSingleton<IHostedService, EmptyHostedService>();
                });
                return hostBuilder;
            }

            if (!hasHelp && args.Length == 1 && args[0].Equals(HelpCommand, StringComparison.OrdinalIgnoreCase))
            {
                Console.Write(new CommandHelpBuilder().BuildHelpMessage(method, defaultMethod));

                hostBuilder.ConfigureServices(services =>
                {
                    services.AddOptions<ConsoleLifetimeOptions>().Configure(x => x.SuppressStatusMessages = true);
                    services.AddSingleton<IHostedService, EmptyHostedService>();
                });
                return hostBuilder;
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

        public static Task RunConsoleAppEngineAsync<T>(this IHostBuilder hostBuilder, string[] args, IConsoleAppInterceptor? interceptor = null)
            where T : ConsoleAppBase
        {
            return UseConsoleAppEngine<T>(hostBuilder, args, interceptor).Build().RunAsync();
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

        static (Type?, MethodInfo?) GetTypeFromAssemblies(string arg0)
        {
            var consoleAppBaseTypes = GetConsoleAppTypes();
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
                    if (cmdattr.CommandNames.Any(x => arg0.Equals(x, StringComparison.OrdinalIgnoreCase)))
                    {
                        if(foundType != null && foundMethod != null)
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
            if(foundType != null && foundMethod != null)
            {
                return (foundType, foundMethod);
            }
            return (null, null);

        }
    }
}