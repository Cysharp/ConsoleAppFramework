using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleAppFramework
{
    public class ConsoleApp
    {
        // Keep this reference as ConsoleApOptions.CommandDescriptors.
        readonly CommandDescriptorCollection commands;
        readonly ConsoleAppOptions options;
        readonly string[] invalidMethodNames =
        {
            "Dispose",
            "DisposeAsync",
            "GetType",
            "ToString",
            "Equals",
            "GetHashCode"
        };

        public IHost Host { get; }
        public ILogger<ConsoleApp> Logger { get; }
        public IServiceProvider Services => Host.Services;
        public IConfiguration Configuration => Host.Services.GetRequiredService<IConfiguration>();
        public IHostEnvironment Environment => Host.Services.GetRequiredService<IHostEnvironment>();
        public IHostApplicationLifetime Lifetime => Host.Services.GetRequiredService<IHostApplicationLifetime>();

        internal ConsoleApp(IHost host)
        {
            this.Host = host;
            this.Logger = host.Services.GetRequiredService<ILogger<ConsoleApp>>();
            this.options = host.Services.GetRequiredService<ConsoleAppOptions>();
            this.commands = options.CommandDescriptors;
        }

        // Statics

        public static ConsoleApp Create(string[] args)
        {
            return CreateBuilder(args).Build();
        }

        public static ConsoleApp Create(string[] args, Action<ConsoleAppOptions> configureOptions)
        {
            return CreateBuilder(args, configureOptions).Build();
        }

        public static ConsoleApp Create(string[] args, Action<HostBuilderContext, ConsoleAppOptions> configureOptions)
        {
            return CreateBuilder(args, configureOptions).Build();
        }

        public static ConsoleAppBuilder CreateBuilder(string[] args)
        {
            return new ConsoleAppBuilder(args, Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args));
        }

        public static ConsoleAppBuilder CreateBuilder(string[] args, Action<ConsoleAppOptions> configureOptions)
        {
            return new ConsoleAppBuilder(args, Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args), configureOptions);
        }

        public static ConsoleAppBuilder CreateBuilder(string[] args, Action<HostBuilderContext, ConsoleAppOptions> configureOptions)
        {
            return new ConsoleAppBuilder(args, Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args), configureOptions);
        }

        public static ConsoleApp CreateFromHostBuilder(IHostBuilder hostBuilder, string[] args)
        {
            return new ConsoleAppBuilder(args, hostBuilder).Build();
        }

        public static ConsoleApp CreateFromHostBuilder(IHostBuilder hostBuilder, string[] args, Action<ConsoleAppOptions> configureOptions)
        {
            return new ConsoleAppBuilder(args, hostBuilder, configureOptions).Build();
        }

        public static ConsoleApp CreateFromHostBuilder(IHostBuilder hostBuilder, string[] args, Action<HostBuilderContext, ConsoleAppOptions> configureOptions)
        {
            return new ConsoleAppBuilder(args, hostBuilder, configureOptions).Build();
        }

        public static void Run(string[] args, Delegate rootCommand)
        {
            RunAsync(args, rootCommand).GetAwaiter().GetResult();
        }

        public static Task RunAsync(string[] args, Delegate rootCommand)
        {
            return Create(args).AddRootCommand(rootCommand).RunAsync();
        }

        public static void Run<T>(string[] args)
            where T : ConsoleAppBase
        {
            Create(args).AddCommands<T>().Run();
        }

        // Add Command

        public ConsoleApp AddRootCommand(Delegate command)
        {
            var attr = command.Method.GetCustomAttribute<CommandAttribute>();
            commands.AddRootCommand(new CommandDescriptor(CommandType.DefaultCommand, command.Method, command.Target, attr));
            return this;
        }

        public ConsoleApp AddRootCommand(string description, Delegate command)
        {
            var attr = new CommandAttribute("root-command", description);
            commands.AddRootCommand(new CommandDescriptor(CommandType.DefaultCommand, command.Method, command.Target, attr));
            return this;
        }

        public ConsoleApp AddCommand(string commandName, Delegate command)
        {
            var attr = new CommandAttribute(commandName);
            commands.AddCommand(new CommandDescriptor(CommandType.Command, command.Method, command.Target, attr));
            return this;
        }

        public ConsoleApp AddCommand(string commandName, string description, Delegate command)
        {
            var attr = new CommandAttribute(commandName, description);
            commands.AddCommand(new CommandDescriptor(CommandType.Command, command.Method, command.Target, attr));
            return this;
        }

        public ConsoleApp AddCommands<T>()
            where T : ConsoleAppBase
        {
            var methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => !m.IsSpecialName);

            foreach (var method in methods)
            {
                if (invalidMethodNames.Contains(method.Name)) continue;

                if (method.GetCustomAttribute<RootCommandAttribute>() != null || (options.NoAttributeCommandAsImplicitlyDefault && method.GetCustomAttribute<CommandAttribute>() == null))
                {
                    var command = new CommandDescriptor(CommandType.DefaultCommand, method);
                    commands.AddRootCommand(command);
                }
                else
                {
                    var command = new CommandDescriptor(CommandType.Command, method);
                    commands.AddCommand(command);
                }
            }
            return this;
        }

        public ConsoleApp AddSubCommand(string parentCommandName, string commandName, Delegate command)
        {
            var attr = new CommandAttribute(commandName);
            commands.AddSubCommand(parentCommandName, new CommandDescriptor(CommandType.SubCommand, command.Method, command.Target, attr, parentCommandName));
            return this;
        }

        public ConsoleApp AddSubCommand(string parentCommandName, string commandName, string description, Delegate command)
        {
            var attr = new CommandAttribute(commandName, description);
            commands.AddSubCommand(parentCommandName, new CommandDescriptor(CommandType.SubCommand, command.Method, command.Target, attr, parentCommandName));
            return this;
        }

        public ConsoleApp AddSubCommands<T>()
        {
            var methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => !m.IsSpecialName);

            var rootName = typeof(T).GetCustomAttribute<CommandAttribute>()?.CommandNames[0] ?? options.NameConverter(typeof(T).Name);

            foreach (var method in methods)
            {
                if (invalidMethodNames.Contains(method.Name)) continue;

                if (method.GetCustomAttribute<RootCommandAttribute>() != null)
                {
                    var command = new CommandDescriptor(CommandType.DefaultCommand, method);
                    commands.AddRootCommand(command);
                }
                else
                {
                    var command = new CommandDescriptor(CommandType.SubCommand, method, parentCommand: rootName);
                    commands.AddSubCommand(rootName, command);
                }
            }
            return this;
        }

        public ConsoleApp AddAllCommandType()
        {
            return AddAllCommandType(AppDomain.CurrentDomain.GetAssemblies());
        }

        public ConsoleApp AddAllCommandType(params Assembly[] searchAssemblies)
        {
            foreach (var type in GetConsoleAppTypes(searchAssemblies))
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                var rootName = type.GetCustomAttribute<CommandAttribute>()?.CommandNames[0] ?? options.NameConverter(type.Name);
                foreach (var method in methods)
                {
                    if (method.Name == "Dispose" || method.Name == "DisposeAsync") continue; // ignore IDisposable

                    commands.AddSubCommand(rootName, new CommandDescriptor(CommandType.SubCommand, method, parentCommand: rootName));
                }
            }
            return this;
        }

        // Run

        public void Run()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        // Don't use return RunAsync to keep stacktrace.
        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            commands.TryAddDefaultHelpMethod();
            commands.TryAddDefaultVersionMethod();

            await Host.RunAsync(cancellationToken);
        }

        static List<Type> GetConsoleAppTypes(Assembly[] searchAssemblies)
        {
            List<Type> consoleAppBaseTypes = new List<Type>();

            foreach (var asm in searchAssemblies)
            {
                if (asm.FullName!.StartsWith("System") || asm.FullName.StartsWith("Microsoft.Extensions") || asm.GetName().Name == "ConsoleAppFramework") continue;

                Type?[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types ?? Array.Empty<Type>();
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
    }
}