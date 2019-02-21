using System.Linq;
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

        public static Task RunBatchEngine(this IHostBuilder hostBuilder, string[] args, IBatchInterceptor interceptor = null)
        {
            if (args.Length == 1 && args[0].Equals(ListCommand, StringComparison.OrdinalIgnoreCase))
            {
                ShowMethodList();
                return Task.CompletedTask;
            }
            if (args.Length == 2 && args[0].Equals(HelpCommand, StringComparison.OrdinalIgnoreCase))
            {
                var (t, mi) = GetTypeFromAssemblies(args[1]);
                if (mi != null)
                {
                    Console.WriteLine(BuildHelpParameter(mi));
                }
                else
                {
                    Console.WriteLine("Method not found , please check \"list\" command.");
                }
                return Task.CompletedTask;
            }

            Type type = null;
            MethodInfo methodInfo = null;
            if (args.Length >= 1)
            {
                (type, methodInfo) = GetTypeFromAssemblies(args[0]);
            }

            return hostBuilder.ConfigureServices(services =>
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
            })
            .RunConsoleAsync();
        }

        public static Task RunBatchEngine<T>(this IHostBuilder hostBuilder, string[] args, IBatchInterceptor interceptor = null)
            where T : BatchBase
        {
            if (args.Length == 1 && args[0].Equals(ListCommand, StringComparison.OrdinalIgnoreCase))
            {
                ShowMethodList();
                return Task.CompletedTask;
            }
            if (args.Length == 1 && args[0].Equals(HelpCommand, StringComparison.OrdinalIgnoreCase))
            {
                var method = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).First();
                Console.WriteLine(BuildHelpParameter(method));
                return Task.CompletedTask;
            }

            return hostBuilder.ConfigureServices(services =>
            {
                services.AddOptions<ConsoleLifetimeOptions>().Configure(x => x.SuppressStatusMessages = true);
                services.AddSingleton<string[]>(args);
                services.AddSingleton<Type>(typeof(T));
                services.AddSingleton<IHostedService, BatchEngineService>();
                services.AddSingleton<IBatchInterceptor>(interceptor ?? NullBatchInerceptor.Default);
                services.AddTransient<T>();
            })
            .RunConsoleAsync();
        }

        static void ShowMethodList()
        {
            var list = GetBatchTypes();
            foreach (var item in list)
            {
                foreach (var item2 in item.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    Console.WriteLine(item.Name + "." + item2.Name);
                }
            }
        }

        static string BuildHelpParameter(MethodInfo method)
        {
            var sb = new StringBuilder();
            foreach (var item in method.GetParameters())
            {
                // -i, -input | [default=foo]...

                var option = item.GetCustomAttribute<OptionAttribute>();

                if (option != null)
                {
                    sb.Append("-" + option.ShortName.Trim('-') + ", ");
                }

                sb.Append("-" + item.Name);
                sb.Append(": ");

                if (item.HasDefaultValue)
                {
                    sb.Append("[default=" + item.DefaultValue.ToString() + "]");
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
    }
}