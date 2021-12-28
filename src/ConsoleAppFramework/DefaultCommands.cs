using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ConsoleAppFramework
{
    internal class DefaultCommands : ConsoleAppBase
    {
        public const string Help = "help";
        public const string Version = "version";

        public static readonly CommandDescriptor HelpCommand = new CommandDescriptor(CommandType.Command, typeof(DefaultCommands).GetMethod(nameof(ShowHelp), BindingFlags.Public | BindingFlags.Instance)!);
        public static readonly CommandDescriptor VersionCommand = new CommandDescriptor(CommandType.Command, typeof(DefaultCommands).GetMethod(nameof(ShowVersion), BindingFlags.Public | BindingFlags.Instance)!);

        readonly ConsoleAppOptions options;
        readonly IServiceProviderIsService isService;

        public DefaultCommands(ConsoleAppOptions options, IServiceProviderIsService isService)
        {
            this.options = options;
            this.isService = isService;
        }

        [Command("help", "Display help.")]
        public void ShowHelp()
        {
            var descriptors = options.CommandDescriptors.GetAllDescriptors();
            if (!options.ShowDefaultCommand)
            {
                descriptors = descriptors.Where(x => x != HelpCommand && x != VersionCommand);
            }
            var message = new CommandHelpBuilder(null, options.StrictOption, isService).BuildHelpMessage(options.CommandDescriptors.GetDefaultCommandDescriptor(), descriptors, shortCommandName: false);
            Console.WriteLine(message);
        }

        [Command("version", "Display version.")]
        public void ShowVersion()
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
    }
}