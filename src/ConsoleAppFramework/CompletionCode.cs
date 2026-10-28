namespace ConsoleAppFramework;

// Runtime completion support is injected alongside the parser, with no runtime dependency.
internal static class CompletionCode
{
    public const string Source = """"

internal sealed record CompletionCandidate(string Value, string Description = "");
internal sealed record CompletionContext(string CommandName, string ParameterName, string WordToComplete, string[] Arguments);

internal static partial class ConsoleApp
{
    /// <summary>Enables environment-activated shell completion for ConsoleApp.Run/RunAsync.</summary>
    public static bool CompletionEnabled { get; set; }
    public static Func<CompletionContext, System.Collections.Generic.IEnumerable<CompletionCandidate>>? CompletionProvider { get; set; }

    // COMPLETE selects a shell; '--' separates adapter requests from registration.
    static bool IsCompletionRequested => global::System.Environment.GetEnvironmentVariable("COMPLETE") is string shell && shell != "" && shell != "0";
    sealed record CompletionParameter(string Name, string[] Names, int Index, bool Flag, bool Variadic, bool Hidden, string Description, string[] Values);
    sealed record CompletionCommand(string Name, string Description, CompletionParameter[] Parameters);

    internal partial class ConsoleAppBuilder
    {
        bool completionEnabled;
        Func<CompletionContext, System.Collections.Generic.IEnumerable<CompletionCandidate>>? completionProvider;

        /// <summary>Enables dynamic shell completion without executing commands or filters.</summary>
        public ConsoleAppBuilder UseCompletion(Func<CompletionContext, System.Collections.Generic.IEnumerable<CompletionCandidate>>? provider = null)
        {
            completionEnabled = true;
            completionProvider = provider;
            return this;
        }

        // A partial hook keeps single-command applications independent of builder metadata.
        partial void CompleteCore(string[] args, ref bool handled);
        bool TryComplete(string[] args)
        {
            if (!completionEnabled || !IsCompletionRequested) return false;
            var handled = false;
            CompleteCore(args, ref handled);
            return handled;
        }
    }

    static void Complete(string[] args, CompletionCommand[] commands, Func<CompletionContext, System.Collections.Generic.IEnumerable<CompletionCandidate>>? provider)
    {
        // Validate the requested adapter before emitting shell-specific output.
        var shell = global::System.Environment.GetEnvironmentVariable("COMPLETE")!;
        if (shell == "pwsh") shell = "powershell";
        if (shell is not ("bash" or "powershell" or "zsh" or "fish"))
        {
            global::System.Console.Error.WriteLine("Unsupported completion shell: " + shell);
            global::System.Environment.ExitCode = 1;
            return;
        }

        // Adapters pass the full command line, including argv[0], after '--'.
        if (args.Length == 0 || args[0] != "--")
        {
            WriteCompletionScript(shell);
            return;
        }

        // A missing final token represents completion immediately after whitespace.
        var words = args.AsSpan(1);
        var indexText = global::System.Environment.GetEnvironmentVariable("_CONSOLEAPP_COMPLETE_INDEX");
        var index = int.TryParse(indexText, out var cursor) ? cursor : words.Length - 1;
        if (index < 1 || index > words.Length) return;
        var word = index < words.Length ? words[index] : "";
        args = words.Slice(1, index - 1).ToArray();

        // Only words preceding the cursor affect command selection and pending values.
        var path = "";
        var position = 0;
        var escaped = false;
        CompletionParameter? pending = null;
        var used = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

        // Walk only the words before the cursor; tolerate incomplete and invalid input.
        foreach (var token in args)
        {
            if (token == "--") { escaped = true; pending = null; continue; }
            if (pending != null)
            {
                if (!pending.Variadic) pending = null;
                continue;
            }

            // Consume option values before treating subsequent words as subcommands.
            if (!escaped && token.StartsWith("-", StringComparison.Ordinal))
            {
                var option = FindParameter(commands, path, token);
                if (option != null)
                {
                    foreach (var name in option.Names) used.Add(name);
                    if (!option.Flag) pending = option;
                }
                continue;
            }

            // Intermediate nodes are completable even without an executable parent command.
            var next = path.Length == 0 ? token : path + " " + token;
            var subcommand = false;
            if (!escaped)
                foreach (var command in commands)
                    if (command.Name == next || command.Name.StartsWith(next + " ", StringComparison.Ordinal)) { subcommand = true; break; }
            if (subcommand) { path = next; position = 0; }
            else position++;
        }

        // Escaped arguments belong to the command and are not parsed by the framework.
        if (escaped) return;
        var optionValue = pending != null;
        var candidates = new System.Collections.Generic.List<CompletionCandidate>();
        CompletionCommand? active = null;
        foreach (var command in commands) if (command.Name == path) { active = command; break; }
        if (pending == null && active != null && !word.StartsWith("-", StringComparison.Ordinal))
            foreach (var parameter in active.Parameters)
                if (parameter.Index == position || (parameter.Variadic && parameter.Index >= 0 && position >= parameter.Index)) { pending = parameter; break; }

        // Value providers run only when a particular parameter expects a value.
        if (pending != null)
        {
            if (!pending.Hidden)
            {
                foreach (var value in pending.Values) candidates.Add(new CompletionCandidate(value));
                if (provider != null)
                    candidates.AddRange(provider(new CompletionContext(path, pending.Name, word, args)));
            }
        }

        // Positional values can be offered alongside commands and optional flags.
        if (!optionValue)
        {
            var prefix = path.Length == 0 ? "" : path + " ";
            foreach (var command in commands)
            {
                if (!command.Name.StartsWith(prefix, StringComparison.Ordinal) || command.Name.Length == prefix.Length) continue;
                var remainder = command.Name.Substring(prefix.Length);
                var space = remainder.IndexOf(' ');
                candidates.Add(new CompletionCandidate(space < 0 ? remainder : remainder.Substring(0, space), command.Description));
            }

            // Global options have no original parameter name and apply at intermediate nodes too.
            foreach (var command in commands)
                foreach (var parameter in command.Parameters)
                    if (!parameter.Hidden && parameter.Index < 0 && (command == active || parameter.Name == ""))
                        foreach (var name in parameter.Names)
                            if (!used.Contains(name)) candidates.Add(new CompletionCandidate(name, parameter.Description));
            candidates.Add(new CompletionCandidate("--help", "Show help"));
            candidates.Add(new CompletionCandidate("-h", "Show help"));
            candidates.Add(new CompletionCandidate("--version", "Show version"));
        }

        // Filter in the executable because PowerShell does not filter returned candidates.
        var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var separator = global::System.Environment.GetEnvironmentVariable("_CONSOLEAPP_IFS") ?? "\n";
        var first = true;
        foreach (var candidate in candidates)
        {
            if (!candidate.Value.StartsWith(word, StringComparison.OrdinalIgnoreCase) || !seen.Add(candidate.Value)) continue;
            if (candidate.Value.AsSpan().IndexOfAny("\r\n\t\v\0".AsSpan()) >= 0) continue;
            var description = candidate.Description.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Replace('\v', ' ');

            // Each shell consumes its native display format; avoid Windows CRLF in Unix shells.
            var value = candidate.Value;
            if (shell == "zsh") value = value.Replace("\\", "\\\\").Replace(":", "\\:") + ":" + description.Replace("\\", "\\\\");
            else if (shell is "powershell" or "fish") value += "\t" + (description.Length == 0 ? candidate.Value : description);
            if (!first) global::System.Console.Write(separator);
            global::System.Console.Write(value);
            first = false;
        }
        if (!first && shell is ("powershell" or "fish")) global::System.Console.Write("\n");
    }

    static CompletionParameter? FindParameter(CompletionCommand[] commands, string path, string token)
    {
        foreach (var command in commands)
            foreach (var parameter in command.Parameters)
                if (command.Name == path || parameter.Name == "")
                    foreach (var name in parameter.Names)
                        if (name == token) return parameter;
        return null;
    }

    static void WriteCompletionScript(string shell)
    {
        var executable = global::System.Environment.GetCommandLineArgs()[0];
        if (executable.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            // Framework-dependent apphosts sit beside their managed entry assembly.
            executable = System.IO.Path.ChangeExtension(executable, OperatingSystem.IsWindows() ? ".exe" : null);
        }

        // Quote once for each shell's parser; Fish has different single-quote escapes.
        var name = System.IO.Path.GetFileName(executable);
        static string Quote(string value, string target) => "'" + (target == "fish"
            ? value.Replace("\\", "\\\\").Replace("'", "\\'")
            : value.Replace("'", target == "powershell" ? "''" : "'\\''")) + "'";
        var quotedExecutable = Quote(executable, shell);
        var quotedName = Quote(name, shell);

        // Keep shell syntax in dedicated templates, independent of command metadata.
        var script = shell switch
        {
            "powershell" => PowerShellCompletion,
            "bash" => BashCompletion,
            "zsh" => ZshCompletion,
            _ => FishCompletion
        };

        // Readable, shell-safe names allow several applications to register together.
        var function = new StringBuilder("_consoleapp_complete_");
        foreach (var ch in name)
            function.Append(ch is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' ? ch : '_');
        global::System.Console.Write(script.Replace("__FUNCTION__", function.ToString()).Replace("__EXECUTABLE__", quotedExecutable).Replace("__NAME__", quotedName).ReplaceLineEndings("\n") + "\n");
    }

    // Shell adapters follow clap_complete's CompleteEnv conventions:
    // https://github.com/clap-rs/clap/blob/master/clap_complete/src/env/shells.rs
    const string BashCompletion = """
__FUNCTION__() {
    local IFS=$'\013'
    local index=${COMP_CWORD}
    local comp_type=${COMP_TYPE}
    local space=true
    if compopt +o nospace 2>/dev/null; then
        space=false
    fi

    # Bash supplies the current fragment separately when word-break characters occur.
    local words=("${COMP_WORDS[@]}")
    if [[ ${BASH_VERSINFO[0]} -ge 4 ]]; then
        words[COMP_CWORD]="$2"
    fi
    COMPREPLY=( $(
        _CONSOLEAPP_IFS="$IFS" \
        _CONSOLEAPP_COMPLETE_INDEX="$index" \
        _CONSOLEAPP_COMPLETE_COMP_TYPE="$comp_type" \
        _CONSOLEAPP_COMPLETE_SPACE="$space" \
        COMPLETE=bash __EXECUTABLE__ -- "${words[@]}"
    ) )

    # Leave failed requests to Bash; avoid adding spaces after path separators.
    if [[ $? != 0 ]]; then
        unset COMPREPLY
    elif [[ $space == false && ${COMPREPLY-} =~ [=/:]$ ]]; then
        compopt -o nospace
    fi
}
if [[ ${BASH_VERSINFO[0]} -gt 4 || ( ${BASH_VERSINFO[0]} -eq 4 && ${BASH_VERSINFO[1]} -ge 4 ) ]]; then
    complete -o nospace -o bashdefault -o nosort -F __FUNCTION__ __NAME__
else
    complete -o nospace -o bashdefault -F __FUNCTION__ __NAME__
fi
""";

    const string PowerShellCompletion = """
Register-ArgumentCompleter -Native -CommandName __NAME__ -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)
    $arguments = @($commandAst.GetCommandName())
    $word = ''

    # Extract literal words without evaluating expressions in the user's command line.
    foreach ($element in $commandAst.CommandElements | Select-Object -Skip 1) {
        if ($element.Extent.StartOffset -ge $cursorPosition) { break }
        if ($element.Extent.EndOffset -ge $cursorPosition) {
            if ($element.Extent.EndOffset -eq $cursorPosition -and $element -is [System.Management.Automation.Language.StringConstantExpressionAst]) { $word = $element.Value }
            else { $word = $element.Extent.Text.Substring(0, $cursorPosition - $element.Extent.StartOffset).TrimStart([char[]]"'`"") }
            break
        }

        # Preserve quotes and whitespace through the AST's decoded literal value.
        if ($element -is [System.Management.Automation.Language.StringConstantExpressionAst]) { $arguments += $element.Value }
        else { $arguments += $element.Extent.Text }
    }

    # Include the current word and pass its index, including an empty trailing word.
    $arguments += $word
    $saved = @($env:COMPLETE, $env:_CONSOLEAPP_COMPLETE_INDEX)
    try {
        $env:COMPLETE = 'powershell'
        $env:_CONSOLEAPP_COMPLETE_INDEX = [string]($arguments.Count - 1)
        & __EXECUTABLE__ -- @arguments | ForEach-Object {
            $parts = $_ -split "`t", 2
            $value = $parts[0]
            $text = $value
            if ($text -match '[\s`"''$;&|<>(){}\[\]*?!#@,]') { $text = "'" + $text.Replace("'", "''") + "'" }
            $description = if ($parts.Count -gt 1 -and $parts[1]) { $parts[1] } else { $value }
            [System.Management.Automation.CompletionResult]::new($text, $value, 'ParameterValue', $description)
        }
    } finally {
        # Restore the caller's environment even if the native process fails.
        $env:COMPLETE = $saved[0]
        $env:_CONSOLEAPP_COMPLETE_INDEX = $saved[1]
    }
}
""";

    const string ZshCompletion = """
#compdef __NAME__
__FUNCTION__() {
    local index=$((CURRENT - 1))
    local -a candidates
    candidates=("${(@f)$(
        _CONSOLEAPP_IFS=$'\n' \
        _CONSOLEAPP_COMPLETE_INDEX="$index" \
        COMPLETE=zsh __EXECUTABLE__ -- "${words[@]}" 2>/dev/null
    )}")

    # _describe handles escaped colons and presents help alongside candidate values.
    local -a dirs=() other=()
    local candidate value
    for candidate in "${candidates[@]}"; do
        value="${candidate%%:*}"
        if [[ "$value" == */ ]]; then
            dirs+=("${value%/}:${candidate#*:}")
        else
            other+=("$candidate")
        fi
    done
    [[ -n $dirs ]] && _describe -V 'values' dirs -S '/' -r '/'
    [[ -n $other ]] && _describe -V 'values' other
}
compdef __FUNCTION__ __NAME__
""";

    const string FishCompletion = """
function __FUNCTION__
    set -l words (commandline --current-process --tokenize --cut-at-cursor)
    set -l word (commandline --current-token --cut-at-cursor)
    set -l index (count $words)
    env COMPLETE=fish _CONSOLEAPP_COMPLETE_INDEX=$index __EXECUTABLE__ -- $words "$word"
end
complete --keep-order --exclusive --command __NAME__ --arguments '(__FUNCTION__)'
""";
}
"""";
}
