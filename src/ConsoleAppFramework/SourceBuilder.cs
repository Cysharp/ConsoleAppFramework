using System.Text;

namespace ConsoleAppFramework;

// indent-level: 0. using namespace, class, struct declaration
// indent-level: 1. field, property, method declaration
// indent-level: 2. method body
internal class SourceBuilder(int level)
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

    public Scope BeginIndent()
    {
        Indent();
        return new Scope(this);
    }

    public Scope BeginIndent(string code)
    {
        AppendLine(code);
        Indent();
        return new Scope(this);
    }

    public Block BeginBlock()
    {
        AppendLine("{");
        Indent();
        return new Block(this);
    }

    public Block BeginBlock(string code)
    {
        AppendLine(code);
        AppendLine("{");
        Indent();
        return new Block(this);
    }

    public IDisposable Nop => NullDisposable.Instance;

    public void AppendLine()
    {
        builder.AppendLine();
    }

    public void AppendLineIfExists<T>(ReadOnlySpan<T> values)
    {
        if (values.Length != 0)
        {
            builder.AppendLine();
        }
    }

    public void AppendLine(string text)
    {
        if (level != 0)
        {
            builder.Append(' ', level * 4); // four spaces
        }
        builder.AppendLine(text);
    }

    public void AppendWithoutIndent(string text)
    {
        builder.Append(text);
    }

    public void AppendLineWithoutIndent(string text)
    {
        builder.AppendLine(text);
    }

    public override string ToString() => builder.ToString();

    public struct Scope(SourceBuilder parent) : IDisposable
    {
        public void Dispose()
        {
            parent.Unindent();
        }
    }

    public struct Block(SourceBuilder parent) : IDisposable
    {
        public void Dispose()
        {
            parent.Unindent();
            parent.AppendLine("}");
        }
    }

    public SourceBuilder Clone()
    {
        var sb = new SourceBuilder(level);
        sb.builder.Append(builder.ToString());
        return sb;
    }

    class NullDisposable : IDisposable
    {
        public static readonly IDisposable Instance = new NullDisposable();

        public void Dispose()
        {
        }
    }
}
