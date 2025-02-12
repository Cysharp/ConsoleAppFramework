using Microsoft.CodeAnalysis;

namespace ConsoleAppFramework;

public class SyntaxNodeTextEqualityComparer : IEqualityComparer<SyntaxNode>
{
    public static readonly SyntaxNodeTextEqualityComparer Default = new SyntaxNodeTextEqualityComparer();

    SyntaxNodeTextEqualityComparer()
    {
    }

    public bool Equals(SyntaxNode? x, SyntaxNode? y)
    {
        if (x == null & y == null) return true;
        if (x == null || y == null) return false;

        using var xWriter = PooledStringWriter.Rent();
        using var yWriter = PooledStringWriter.Rent();

        x.WriteTo(xWriter);
        y.WriteTo(yWriter);

        return xWriter.AsSpan().SequenceEqual(yWriter.AsSpan());
    }

    public bool Equals(SyntaxList<SyntaxNode> xs, SyntaxList<SyntaxNode> ys)
    {
        if (xs.Count != ys.Count) return false;

        using var xWriter = PooledStringWriter.Rent();
        using var yWriter = PooledStringWriter.Rent();

        for (int i = 0; i < xs.Count; i++)
        {
            var x = xs[i];
            var y = ys[i];

            x.WriteTo(xWriter);
            y.WriteTo(yWriter);

            if (!xWriter.AsSpan().SequenceEqual(yWriter.AsSpan()))
            {
                return false;
            }

            xWriter.Clear();
            yWriter.Clear();
        }

        return true;
    }

    public int GetHashCode(SyntaxNode obj)
    {
        if (obj == null) return 0;

        using var writer = PooledStringWriter.Rent();
        obj.WriteTo(writer);

        var span = writer.AsSpan();

        // simple hashing
        int hash = 0;
        for (int i = 0; i < span.Length; i++)
        {
            hash = (hash * 37) ^ span[i];
        }

        return hash;
    }
}
