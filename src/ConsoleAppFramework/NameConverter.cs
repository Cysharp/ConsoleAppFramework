using System.Text;

namespace ConsoleAppFramework;

public static class NameConverter
{
    public static string ToKebabCase(string name)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (!Char.IsUpper(name[i]))
            {
                sb.Append(name[i]);
                continue;
            }

            // Abc, abC, AB-c => first or Last or capital continuous, no added.
            if (i == 0 || i == name.Length - 1 || Char.IsUpper(name[i + 1]))
            {
                sb.Append(Char.ToLowerInvariant(name[i]));
                continue;
            }

            // others, add-
            sb.Append('-');
            sb.Append(Char.ToLowerInvariant(name[i]));
        }
        return sb.ToString();
    }
}
