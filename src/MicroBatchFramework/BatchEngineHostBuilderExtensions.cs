using System.Linq;
using MicroBatchFramework.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace MicroBatchFramework
{
    public static class BatchEngineHostBuilderExtensions
    {
        const string ListCommand = "list";
        const string HelpCommand = "help";

        public static IHostBuilder UseBatchEngine(this IHostBuilder hostBuilder, string[] args, IBatchInterceptor interceptor = null, bool useSimpleConosoleLogger = true)
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
                    Console.WriteLine(BuildHelpParameter(new[] { mi }));
                }
                else
                {
                    Console.WriteLine("Method not found , please check \"list\" command.");
                }
                hostBuilder.ConfigureServices(services =>
                {
                    services.AddOptions<ConsoleLifetimeOptions>().Configure(x => x.SuppressStatusMessages = true);
                    services.AddSingleton<IHostedService, EmptyHostedService>();
                });
                return hostBuilder;
            }

            Type type = null;
            MethodInfo methodInfo = null;
            if (args.Length >= 1)
            {
                (type, methodInfo) = GetTypeFromAssemblies(args[0]);
            }

            hostBuilder = hostBuilder
                .ConfigureServices(services =>
                {
                    services.AddOptions<ConsoleLifetimeOptions>().Configure(x => x.SuppressStatusMessages = true);
                    services.AddSingleton<string[]>(args);
                    services.AddSingleton<IHostedService, BatchEngineService>();
                    services.AddSingleton<IBatchInterceptor>(interceptor ?? NullBatchInerceptor.Default);
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

            if (useSimpleConosoleLogger)
            {
                hostBuilder = hostBuilder.ConfigureLogging(x => x.AddSimpleConsole());
            }

            return hostBuilder.UseConsoleLifetime();
        }

        public static Task RunBatchEngineAsync(this IHostBuilder hostBuilder, string[] args, IBatchInterceptor interceptor = null, bool useSimpleConosoleLogger = true)
        {
            return UseBatchEngine(hostBuilder, args, interceptor, useSimpleConosoleLogger).Build().RunAsync();
        }

        public static IHostBuilder UseBatchEngine<T>(this IHostBuilder hostBuilder, string[] args, IBatchInterceptor interceptor = null, bool useSimpleConosoleLogger = true)
            where T : BatchBase
        {
            if (args.Length == 0)
            {
                var method = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var defaultMethod = method.FirstOrDefault(x => x.GetCustomAttribute<CommandAttribute>() == null);
                if (defaultMethod == null || defaultMethod.GetParameters().Length != 0)
                {
                    Console.WriteLine(BuildHelpParameter(method));
                    hostBuilder.ConfigureServices(services =>
                    {
                        services.AddOptions<ConsoleLifetimeOptions>().Configure(x => x.SuppressStatusMessages = true);
                        services.AddSingleton<IHostedService, EmptyHostedService>();
                    });
                    return hostBuilder;
                }
            }

            if (args.Length == 1 && args[0].Equals(ListCommand, StringComparison.OrdinalIgnoreCase))
            {
                ShowMethodList();
                hostBuilder.ConfigureServices(services =>
                {
                    services.AddOptions<ConsoleLifetimeOptions>().Configure(x => x.SuppressStatusMessages = true);
                    services.AddSingleton<IHostedService, EmptyHostedService>();
                });
                return hostBuilder;
            }
            if (args.Length == 1 && args[0].Equals(HelpCommand, StringComparison.OrdinalIgnoreCase))
            {
                var method = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                Console.WriteLine(BuildHelpParameter(method));
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
                services.AddSingleton<IHostedService, BatchEngineService>();
                services.AddSingleton<IBatchInterceptor>(interceptor ?? NullBatchInerceptor.Default);
                services.AddTransient<T>();
            });

            if (useSimpleConosoleLogger)
            {
                hostBuilder = hostBuilder.ConfigureLogging(x => x.AddSimpleConsole());
            }

            return hostBuilder.UseConsoleLifetime();
        }

        public static Task RunBatchEngineAsync<T>(this IHostBuilder hostBuilder, string[] args, IBatchInterceptor interceptor = null, bool useSimpleConosoleLogger = true)
            where T : BatchBase
        {
            return UseBatchEngine<T>(hostBuilder, args, interceptor, useSimpleConosoleLogger).Build().RunAsync();
        }

        static void ShowMethodList()
        {
            Console.WriteLine("list of methods:");
            var list = GetBatchTypes();
            foreach (var item in list)
            {
                foreach (var item2 in item.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    Console.WriteLine(item.Name + "." + item2.Name);
                }
            }
        }

        static string BuildHelpParameter(MethodInfo[] methods)
        {
            var sb = new StringBuilder();
            foreach (var method in methods.OrderBy(x => x, new CustomSorter()))
            {
                var command = method.GetCustomAttribute<CommandAttribute>();
                if (command != null)
                {
                    sb.AppendLine(command.CommandName + ": " + command.Description);
                }
                else
                {
                    sb.AppendLine("argument list:");
                }

                var parameters = method.GetParameters();
                if (parameters.Length == 0)
                {
                    sb.AppendLine("()");
                }

                foreach (var item in parameters)
                {
                    // -i, -input | [default=foo]...

                    var option = item.GetCustomAttribute<OptionAttribute>();

                    if (option != null)
                    {
                        if (option.Index != -1)
                        {
                            sb.Append("[" + option.Index + "]");
                            goto WRITE_DESCRIPTION;
                        }
                        else
                        {
                            sb.Append("-" + option.ShortName.Trim('-') + ", ");
                        }
                    }

                    sb.Append("-" + item.Name);

                WRITE_DESCRIPTION:
                    sb.Append(": ");

                    if (item.HasDefaultValue)
                    {
                        sb.Append("[default=" + (item.DefaultValue?.ToString() ?? "null") + "]");
                    }

                    if (option != null && !string.IsNullOrEmpty(option.Description))
                    {
                        sb.Append(option.Description);
                    }
                    else
                    {
                        sb.Append(item.ParameterType.Name);
                    }
                    sb.AppendLine();
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        static List<Type> GetBatchTypes()
        {
            List<Type> batchBaseTypes = new List<Type>();

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
                    if (typeof(BatchBase).IsAssignableFrom(item) && item != typeof(BatchBase))
                    {
                        batchBaseTypes.Add(item);
                    }
                }
            }

            return batchBaseTypes;
        }

        static (Type, MethodInfo) GetTypeFromAssemblies(string arg0)
        {
            var split = arg0.Split('.');
            if (split.Length != 2)
            {
                return (null, null);
            }
            var typeName = split[0];
            var methodName = split[1];

            var batchBaseTypes = GetBatchTypes();
            if (batchBaseTypes == null)
            {
                return (null, null);
            }

            Type foundType = null;
            foreach (var item in batchBaseTypes)
            {
                if (item.Name == typeName)
                {
                    if (foundType != null)
                    {
                        throw new InvalidOperationException("Duplicate BatchBase TypeName is not allowed, " + foundType.FullName + " and " + item.FullName);
                    }
                    foundType = item;
                }
            }

            if (foundType != null)
            {
                var method = foundType.GetMethod(methodName);
                if (method != null)
                {
                    return (foundType, method);
                }
            }

            return (null, null);
        }

        class CustomSorter : IComparer<MethodInfo>
        {
            public int Compare(MethodInfo x, MethodInfo y)
            {
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