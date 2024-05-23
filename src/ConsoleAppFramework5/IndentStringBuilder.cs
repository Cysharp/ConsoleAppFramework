using System.Text;

namespace ConsoleAppFramework;

internal class IndentStringBuilder(int level)
{
    StringBuilder builder = new StringBuilder();

    public void Indent(int levelIncr = 1)
    {
        level += levelIncr;
    }

    public void Unindent(int levelDecr = 1)
    {
        level -= levelDecr;
    }

    public void AppendLine(string text)
    {
        if (level != 0)
        {
            builder.Append(' ', level * 4); // four spaces
        }
        builder.AppendLine(text);
    }

    public void IndentAppendLine(string text)
    {
        Indent();
        AppendLine(text);
    }

    public void UnindentAppendLine(string text)
    {
        Unindent();
        AppendLine(text);
    }

    public void IndentAppendLineUnindent(string text)
    {
        Indent();
        AppendLine(text);
        Unindent();
    }

    public override string ToString()
    {
        return builder.ToString();
    }
}