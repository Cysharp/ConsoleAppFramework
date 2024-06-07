using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppFramework.GeneratorTests;

public class PooledStringWriterTest
{
    [Fact]
    public void AppendChars()
    {
        PooledStringWriter.PoolClear();

        using var writer = PooledStringWriter.Rent();
        var reference = new StringWriter();
        for (int i = 0; i < 300; i++)
        {
            writer.Write((char)i);
            reference.Write((char)i);
        }

        writer.ToString().Equals(reference.ToString());
    }

    [Fact]
    public void AppendStrings()
    {
        PooledStringWriter.PoolClear();

        var str = Guid.NewGuid().ToString();
        using var writer = PooledStringWriter.Rent();
        var reference = new StringWriter();
        for (int i = 0; i < 300; i++)
        {
            writer.Write(str);
            reference.Write(str);
        }

        writer.ToString().Equals(reference.ToString());
    }
}
