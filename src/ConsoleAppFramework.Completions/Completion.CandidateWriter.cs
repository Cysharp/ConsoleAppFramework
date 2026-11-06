using System.Text;

namespace ConsoleAppFramework.Completions;

public static partial class Completion
{
    // Buffer one response instead of making synchronized console writes per candidate.
    struct CandidateWriter(string shell, string prefix)
    {
        readonly HashSet<string> seen = new(StringComparer.Ordinal);
        readonly StringBuilder output = new();
        readonly string separator = Environment.GetEnvironmentVariable("_CONSOLEAPP_IFS") ?? "\n";

        public readonly bool HasCandidates => seen.Count != 0;

        public void Add(string value, string description = "")
        {
            if (!Accepts(value) || !seen.Add(value)) return;
            Append(value, description);
        }

        public void Add(ReadOnlySpan<char> value, string description)
        {
            if (!Accepts(value)) return;
            var text = value.ToString();
            if (seen.Add(text)) Append(value, description);
        }

        // Filter before allocating candidate strings or escaping display text.
        readonly bool Accepts(ReadOnlySpan<char> value) =>
            value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && value.IndexOfAny("\r\n\t\v\0") < 0;

        void Append(ReadOnlySpan<char> value, string description)
        {
            if (seen.Count > 1) output.Append(separator);
            if (shell == "zsh") AppendEscaped(value, true);
            else output.Append(value);
            if (shell == "bash") return;

            // Sanitize descriptions and escape Zsh backslashes without temporary strings.
            output.Append(shell == "zsh" ? ':' : '\t');
            if (description.Length == 0 && shell != "zsh") output.Append(value);
            else AppendEscaped(description, false);
        }

        void AppendEscaped(ReadOnlySpan<char> text, bool value)
        {
            foreach (var character in text)
            {
                if (shell == "zsh" && (character == '\\' || (value && character == ':'))) output.Append('\\');
                output.Append(character is '\r' or '\n' or '\t' or '\v' or '\0' ? ' ' : character);
            }
        }

        public readonly void Flush()
        {
            if (seen.Count == 0) return;
            if (shell is "powershell" or "fish") output.Append('\n');
            Console.Write(output.ToString());
        }
    }
}
