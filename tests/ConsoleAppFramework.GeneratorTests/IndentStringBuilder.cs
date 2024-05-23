using System.Text;

namespace ConsoleAppFramework.GeneratorTests;

public class IndentStringBuilder(int level)
{
    StringBuilder builder = new StringBuilder();

    public void Indent()
    {
        level++;
    }

    public void Unindent()
    {
        level--;
    }

    public void AppendLine(string text)
    {
        if (level != 0)
        {
            builder.Append(' ', 4 * level);
        }
        builder.AppendLine(text);
    }

    public override string ToString()
    {
        return builder.ToString();
    }
}
