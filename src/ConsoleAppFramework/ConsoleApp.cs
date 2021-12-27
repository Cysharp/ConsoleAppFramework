using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        readonly CommandDescriptorMatcher commands;

        public IHost Host { get; }

        internal ConsoleApp(IHost host)
        {
            this.Host = host;
            this.commands = host.Services.GetRequiredService<ConsoleAppOptions>().CommandDescriptors;
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
            return new ConsoleAppBuilder(args);
        }

        public static ConsoleAppBuilder CreateBuilder(string[] args, Action<ConsoleAppOptions> configureOptions)
        {
            return new ConsoleAppBuilder(args, configureOptions);
        }

        public static ConsoleAppBuilder CreateBuilder(string[] args, Action<HostBuilderContext, ConsoleAppOptions> configureOptions)
        {
            return new ConsoleAppBuilder(args, configureOptions);
        }

        public static void Run(string[] args, Delegate defaultCommand)
        {
            Create(args).Run(defaultCommand);
        }

        public static Task RunAsync(string[] args, Delegate defaultCommand)
        {
            return Create(args).RunAsync(defaultCommand);
        }

        public static void Run<T>(string[] args)
            where T : ConsoleAppBase
        {
            Create(args).AddCommands<T>().Run();
        }

        // TODO/ RUn routed???

        // Add Command

        public ConsoleApp AddCommand(string commandName, Delegate command)
        {
            var attr = new CommandAttribute(commandName);
            commands.AddCommand(new CommandDescriptor(command.Method, command.Target, attr));
            return this;
        }

        public ConsoleApp AddCommand(string commandName, string description, Delegate command)
        {
            var attr = new CommandAttribute(commandName, description);
            commands.AddCommand(new CommandDescriptor(command.Method, command.Target, attr));
            return this;
        }

        public ConsoleApp AddCommands<T>()
            where T : ConsoleAppBase
        {
            var methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<CommandAttribute>();
                commands.AddCommand(new CommandDescriptor(method, null, attr));
            }
            return this;
        }

        // TODO:AddSubCommand
        // TODO:AddSubCommands<T>()

        public ConsoleApp AddRoutedCommands()
        {
            return AddRoutedCommands(AppDomain.CurrentDomain.GetAssemblies());
        }

        public ConsoleApp AddRoutedCommands(params Assembly[] searchAssemblies)
        {
            foreach (var type in GetConsoleAppTypes(searchAssemblies))
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttribute<CommandAttribute>();
                    // TODO:type-name get from CommandAttribute
                    commands.AddSubCommand(type.Name, new CommandDescriptor(method, null, attr));
                }
            }
            return this;
        }

        // Run

        public void Run()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        public void Run(Delegate defaultCommand)
        {
            RunAsync().GetAwaiter().GetResult();
        }

        // Don't use return RunAsync to keep stacktrace.
        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            // Start ConsoleAppEngineService.
            await Host.RunAsync(cancellationToken);
        }

        public async Task RunAsync(Delegate defaultCommand, CancellationToken cancellationToken = default)
        {
            var attr = defaultCommand.Method.GetCustomAttribute<CommandAttribute>();
            var descriptor = new CommandDescriptor(defaultCommand.Method, defaultCommand.Target, attr);
            commands.SetDefaultCommand(descriptor);

            await Host.RunAsync(cancellationToken);
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