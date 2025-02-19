namespace ConsoleAppFramework;

internal static class StringExtensions
{
#if NETSTANDARD2_0
    public static string ReplaceLineEndings(this string input)
    {
#pragma warning disable RS1035
        return ReplaceLineEndings(input, Environment.NewLine);
#pragma warning restore RS1035
    }

    public static string ReplaceLineEndings(this string text, string replacementText)
    {
        text = text.Replace("\r\n", "\n");

        if (replacementText != "\n")
            text = text.Replace("\n", replacementText);

        return text;
    }
#endif
}
