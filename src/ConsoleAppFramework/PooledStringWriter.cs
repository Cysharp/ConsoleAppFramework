using System.Collections.Concurrent;
using System.Text;

namespace ConsoleAppFramework;

// based on StringWriter to calc span equality

public sealed class PooledStringWriter : TextWriter
{
    // Pool

    static readonly ConcurrentQueue<PooledStringWriter> pool = new();

    public static PooledStringWriter Rent()
    {
        if (!pool.TryDequeue(out var writer))
        {
            writer = new PooledStringWriter();
        }
        return writer;
    }

    public static void PoolClear()
    {
        while (pool.TryDequeue(out _)) { }
    }

    // Return
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.Clear();
            pool.Enqueue(this);
        }
    }

    // Impl

    static UnicodeEncoding? encoding;

    char[] chars;
    int position;

    PooledStringWriter()
    {
        chars = new char[256];
    }

    public override void Close()
    {
        Dispose(true);
    }

    public void Clear()
    {
        position = 0;
    }

    public ReadOnlySpan<char> AsSpan() => chars.AsSpan(0, position);

    public override Encoding Encoding => encoding ??= new UnicodeEncoding(false, false);

    // Writes a character to the underlying string buffer.
    //
    public override void Write(char value)
    {
        if (chars.Length < position + 1)
        {
            EnsureCapacity(1);
        }

        chars[position++] = value;
    }

    void EnsureCapacity(int length)
    {
        var finalLength = position + length;
        var next = chars.Length * 2;
        var newSize = Math.Max(next, finalLength);
        Array.Resize(ref chars, newSize);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        Write(buffer.AsSpan(index, count));
    }

    public void Write(ReadOnlySpan<char> buffer)
    {
        var dest = chars.AsSpan(position);
        if (dest.Length < buffer.Length)
        {
            EnsureCapacity(buffer.Length);
            dest = chars.AsSpan(position);
        }

        buffer.CopyTo(dest);
        position += buffer.Length;
    }

    public override void Write(string? value)
    {
        if (value != null)
        {
            Write(value.AsSpan());
        }
    }

    #region Task based Async APIs

    public override Task WriteAsync(char value)
    {
        Write(value);
        return Task.CompletedTask;
    }

    public override Task WriteAsync(string? value)
    {
        Write(value);
        return Task.CompletedTask;
    }

    public override Task WriteAsync(char[] buffer, int index, int count)
    {
        Write(buffer, index, count);
        return Task.CompletedTask;
    }

    public override Task WriteLineAsync(char value)
    {
        WriteLine(value);
        return Task.CompletedTask;
    }

    public override Task WriteLineAsync(string? value)
    {
        WriteLine(value);
        return Task.CompletedTask;
    }

    public override Task WriteLineAsync(char[] buffer, int index, int count)
    {
        WriteLine(buffer, index, count);
        return Task.CompletedTask;
    }

    public override Task FlushAsync()
    {
        return Task.CompletedTask;
    }

    #endregion

    public override string ToString()
    {
        return new string(chars, 0, position);
    }
}
