using Microsoft.Extensions.Hosting;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;

namespace ConsoleAppFramework
{
    public static class LegacyCompatibleExtensions
    {
        /// <summary>
        /// Run multiple ConsoleApp that are searched from all assemblies.
        /// </summary>
        // [Obsolete]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task RunConsoleAppFrameworkAsync(this IHostBuilder hostBuilder, string[] args, ConsoleAppOptions? options = null, Assembly[]? searchAssemblies = null)
        {
            options = ConfigureLegacyCompatible(options);
            args = ConfigureLegacyCompatibleArgs(args);

            return new ConsoleAppBuilder(args, hostBuilder, options)
                .Build()
                .AddRoutedCommands(searchAssemblies ?? AppDomain.CurrentDomain.GetAssemblies())
                .RunAsync();
        }

        /// <summary>
        /// Run a single ConsoleApp type that is targeted by type argument.
        /// </summary>
        // [Obsolete]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task RunConsoleAppFrameworkAsync<T>(this IHostBuilder hostBuilder, string[] args, ConsoleAppOptions? options = null)
            where T : ConsoleAppBase
        {
            options = ConfigureLegacyCompatible(options);
            args = ConfigureLegacyCompatibleArgs(args);

            return new ConsoleAppBuilder(args, hostBuilder, options)
                .Build()
                .AddCommands<T>()
                .RunAsync();
        }

        static ConsoleAppOptions ConfigureLegacyCompatible(ConsoleAppOptions? options)
        {
            if (options == null)
            {
                options = new ConsoleAppOptions();
            }

            options.NoAttributeCommandAsImplicitlyDefault = true;
            options.StrictOption = false;
            options.NameConverter = x => x.ToLower();
            options.ReplaceToUseSimpleConsoleLogger = false;
            return options;
        }

        static string[] ConfigureLegacyCompatibleArgs(string[] args)
        {
            if (args.Length >= 1 && args[0].Contains("."))
            {
                var spritCommand = args[0].Split('.');

                var newArgs = new string[args.Length + 1];
                for (int i = 0; i < newArgs.Length; i++)
                {
                    if (i == 0)
                    {
                        newArgs[i] = spritCommand[0];
                    }
                    else if (i == 1)
                    {
                        newArgs[i] = spritCommand[1];
                    }
                    else
                    {
                        newArgs[i] = args[i - 1];
                    }
                }
                return newArgs;
            }

            return args;
        }
    }
}