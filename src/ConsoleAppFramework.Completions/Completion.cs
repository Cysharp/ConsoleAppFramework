namespace ConsoleAppFramework.Completions;

public sealed record CompletionCandidate(string Value, string Description = "");

public sealed record CompletionContext(
    string CommandName,
    string ParameterName,
    string WordToComplete,
    string[] Arguments);

/// <summary>Opt-in shell completion based on CLI Schema metadata.</summary>
public static partial class Completion
{
    /// <summary>Reads the application's schema by default; schemaFactory can override it.</summary>
    /// <param name="hideStaticCompletions">When true, matching dynamic candidates suppress static candidates; otherwise static candidates remain available.</param>
    public static bool TryComplete(this ICliSchemaProvider app, string[] args,
        Func<CompletionContext, IEnumerable<CompletionCandidate>>? provider = null,
        Func<CommandHelpDefinition[]>? schemaFactory = null, bool hideStaticCompletions = false)
    {
        if (!TryBegin(args, out var shell, out var index)) return false;
        if (index >= 0)
            Complete(args, index, schemaFactory == null ? app.GetCliSchema() : schemaFactory(), shell, provider,
                hideStaticCompletions);
        return true;
    }

    /// <summary>Handles completion using a custom, lazily evaluated schema producer.</summary>
    /// <param name="hideStaticCompletions">When true, matching dynamic candidates suppress static candidates; otherwise static candidates remain available.</param>
    public static bool TryHandle(string[] args, Func<CommandHelpDefinition[]> schemaFactory,
        Func<CompletionContext, IEnumerable<CompletionCandidate>>? provider = null, bool hideStaticCompletions = false)
    {
        if (!TryBegin(args, out var shell, out var index)) return false;
        if (index >= 0) Complete(args, index, schemaFactory(), shell, provider, hideStaticCompletions);
        return true;
    }

    static bool TryBegin(string[] args, out string shell, out int index)
    {
        // The normal command path does not create metadata, delegates, or completion state.
        shell = Environment.GetEnvironmentVariable("COMPLETE") ?? "";
        index = -1;
        if (shell.Length == 0 || shell == "0") return false;
        shell = NormalizeShell(shell);
        if (args.Length == 0 || args[0] != "--")
        {
            Console.Write(GenerateScript(shell, GetExecutable()));
            return true;
        }

        // Reject invalid or escaped requests before asking the application for its schema.
        var words = args.AsSpan(1);
        var indexText = Environment.GetEnvironmentVariable("_CONSOLEAPP_COMPLETE_INDEX");
        var cursor = int.TryParse(indexText, out var parsed) ? parsed : words.Length - 1;
        if (cursor < 1 || cursor > words.Length) return true;
        foreach (var token in words.Slice(1, cursor - 1))
            if (token == "--")
                return true;
        index = cursor;
        return true;
    }

    static string NormalizeShell(string shell) => shell switch
    {
        "pwsh" => "powershell",
        "bash" or "zsh" or "fish" or "powershell" => shell,
        _ => throw new ArgumentException("Unsupported completion shell: " + shell, nameof(shell))
    };

    static string GetExecutable()
    {
        var executable = Environment.GetCommandLineArgs()[0];
        return executable.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            ? Path.ChangeExtension(executable, OperatingSystem.IsWindows() ? ".exe" : null)
            : executable;
    }

    static void Complete(string[] args, int index, CommandHelpDefinition[] commands, string shell,
        Func<CompletionContext, IEnumerable<CompletionCandidate>>? provider, bool hideStaticCompletions)
    {
        // Read the schema directly, keeping argv as a span until a provider needs its own array.
        var words = args.AsSpan(1);
        var word = index < words.Length ? words[index] : "";
        var arguments = words.Slice(1, index - 1);
        var globals = GetGlobalOptions(commands);
        FindCommand(commands, "", out var active);
        var path = "";
        var position = 0;
        CommandOptionHelpDefinition? pending = null;
        HashSet<string>? used = null;

        // Parse completed words. Values take precedence over subcommand recognition.
        foreach (var token in arguments)
        {
            if (pending != null)
            {
                if (!pending.IsParams) pending = null;
                continue;
            }

            if (token.StartsWith('-'))
            {
                var option = FindOption(active, globals, token);
                if (option == null) continue;
                used ??= new HashSet<string>(StringComparer.Ordinal);
                foreach (var name in option.Options) used.Add(name);
                if (!option.IsFlag) pending = option;
                continue;
            }

            // Preserve aliases in the provider context while reusing their schema objects.
            var next = path.Length == 0 ? token : path + " " + token;
            if (FindCommand(commands, next, out var command))
            {
                path = next;
                active = command;
                position = 0;
            }
            else position++;
        }

        // Positional values can coexist with option and subcommand suggestions.
        var optionValue = pending != null;
        if (pending == null && active != null && !word.StartsWith('-'))
            foreach (var parameter in active.Options)
                if (parameter.Index == position ||
                    (parameter.IsParams && parameter.Index.HasValue && position >= parameter.Index.Value))
                {
                    pending = parameter;
                    break;
                }

        // Built-in candidates do not allocate candidate records or intermediate lists.
        var writer = new CandidateWriter(shell, word);
        if (pending is { IsHidden: false })
        {
            // First value wins: dynamic candidates override generated values and descriptions.
            if (provider != null)
                foreach (var candidate in provider(new CompletionContext(path, pending.ParameterName, word,
                             arguments.ToArray())))
                    writer.Add(candidate.Value, candidate.Description);
        }

        // Suppress static candidates only if the provider produced a usable, prefix-matching value.
        if (!hideStaticCompletions || !writer.HasCandidates)
        {
            if (pending is { IsHidden: false })
                foreach (var value in pending.AllowedValues)
                    writer.Add(value);
            if (!optionValue) WriteNames(commands, active, globals, path, used, ref writer);
        }

        writer.Flush();
    }

    static Dictionary<string, CommandOptionHelpDefinition>? GetGlobalOptions(CommandHelpDefinition[] commands)
    {
        // Global metadata repeats per command in the schema; index each alias only once.
        Dictionary<string, CommandOptionHelpDefinition>? options = null;
        foreach (var command in commands)
        {
            if (command.IsHidden) continue;
            foreach (var parameter in command.Options)
            {
                if (!parameter.IsGlobal) continue;
                options ??= new Dictionary<string, CommandOptionHelpDefinition>(StringComparer.Ordinal);
                foreach (var name in parameter.Options) options.TryAdd(name, parameter);
            }
        }

        return options;
    }

    static CommandOptionHelpDefinition? FindOption(CommandHelpDefinition? active,
        Dictionary<string, CommandOptionHelpDefinition>? globals, string token)
    {
        // Only inspect the selected command, then use the global alias index.
        if (active != null)
            foreach (var parameter in active.Options)
            {
                if (parameter.Index.HasValue || parameter.IsGlobal) continue;
                foreach (var name in parameter.Options)
                    if (name == token)
                        return parameter;
            }

        return globals != null && globals.TryGetValue(token, out var global) ? global : null;
    }

    static bool FindCommand(CommandHelpDefinition[] commands, string path, out CommandHelpDefinition? active)
    {
        active = null;
        var found = false;
        // A matching prefix is a valid intermediate node even without a command handler.
        foreach (var command in commands)
        {
            if (command.IsHidden) continue;
            foreach (var name in new CommandNames(command.CommandName))
            {
                if (name.SequenceEqual(path))
                {
                    active = command;
                    return true;
                }

                if (IsChild(name, path)) found = true;
            }
        }

        return found;
    }

    static bool IsChild(ReadOnlySpan<char> name, string path) => name.Length > path.Length &&
                                                                 (path.Length == 0 ||
                                                                  (name.StartsWith(path, StringComparison.Ordinal) &&
                                                                   name[path.Length] == ' '));

    static void WriteNames(CommandHelpDefinition[] commands, CommandHelpDefinition? active,
        Dictionary<string, CommandOptionHelpDefinition>? globals, string path, HashSet<string>? used,
        ref CandidateWriter writer)
    {
        // Split aliases and command paths as spans, allocating only accepted candidate values.
        foreach (var command in commands)
        {
            if (command.IsHidden) continue;
            foreach (var name in new CommandNames(command.CommandName))
            {
                if (!IsChild(name, path)) continue;
                var remainder = name[(path.Length == 0 ? 0 : path.Length + 1)..];
                var space = remainder.IndexOf(' ');
                writer.Add(space < 0 ? remainder : remainder[..space], command.Description);
            }
        }

        // Emit local options once and global aliases once, regardless of the number of commands.
        if (active != null)
            foreach (var parameter in active.Options)
            {
                if (parameter.IsHidden || parameter.Index.HasValue || parameter.IsGlobal) continue;
                foreach (var name in parameter.Options)
                    if (used == null || !used.Contains(name))
                        writer.Add(name, parameter.Description);
            }

        if (globals != null)
            foreach (var (name, parameter) in globals)
                if (!parameter.IsHidden && (used == null || !used.Contains(name)))
                    writer.Add(name, parameter.Description);

        // Built-in options also apply at intermediate nodes.
        writer.Add("--help", "Show help");
        writer.Add("-h", "Show help");
        writer.Add("--version", "Show version");
    }

    // Enumerate aliases without Split arrays, iterator objects, or copied parameter metadata.
    ref struct CommandNames(ReadOnlySpan<char> names)
    {
        ReadOnlySpan<char> remaining = names;
        bool finished;
        public ReadOnlySpan<char> Current { get; private set; }
        public readonly CommandNames GetEnumerator() => this;

        public bool MoveNext()
        {
            if (finished) return false;
            var separator = remaining.IndexOf('|');
            Current = (separator < 0 ? remaining : remaining[..separator]).Trim();
            finished = separator < 0;
            remaining = finished ? default : remaining[(separator + 1)..];
            return true;
        }
    }
}
