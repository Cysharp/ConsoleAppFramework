using System.Text;

namespace ConsoleAppFramework.Completions;

public static partial class Completion
{
    /// <summary>Generates a shell adapter that calls the executable for every completion request.</summary>
    public static string GenerateScript(string shell, string executable)
    {
        shell = NormalizeShell(shell);
        var name = Path.GetFileName(executable);
        var function = new StringBuilder("_consoleapp_complete_");
        foreach (var character in name)
            function.Append(char.IsAsciiLetterOrDigit(character) ? character : '_');

        // Select only the requested adapter; command metadata never enters the script.
        var template = shell switch
        {
            "powershell" => PowerShellCompletion,
            "bash" => BashCompletion,
            "zsh" => ZshCompletion,
            _ => FishCompletion
        };

        // Substitute once so a path containing a template marker remains literal.
        return ReplaceTemplate(template, function.ToString(), Quote(executable, shell), Quote(name, shell))
            .ReplaceLineEndings("\n") + "\n";
    }

    static string ReplaceTemplate(ReadOnlySpan<char> template, string function, string executable, string name)
    {
        var output = new StringBuilder(template.Length);
        while (!template.IsEmpty)
        {
            var start = template.IndexOf("__");
            if (start < 0)
            {
                output.Append(template);
                break;
            }

            output.Append(template[..start]);
            template = template[(start + 2)..];
            var end = template.IndexOf("__");

            // Template markers are fixed literals; replacement text is never scanned again.
            output.Append(template[..end] switch
            {
                "FUNCTION" => function,
                "EXECUTABLE" => executable,
                "NAME" => name,
                _ => throw new InvalidOperationException("Unknown completion template marker.")
            });
            template = template[(end + 2)..];
        }

        return output.ToString();
    }

    // Fish and PowerShell use different single-quote escaping rules from Bash and Zsh.
    static string Quote(string value, string shell) => "'" + (shell == "fish"
        ? value.Replace("\\", "\\\\").Replace("'", "\\'")
        : value.Replace("'", shell == "powershell" ? "''" : "'\\''")) + "'";

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
