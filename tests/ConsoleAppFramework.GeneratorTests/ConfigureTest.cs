using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppFramework.GeneratorTests;

public class ConfigureTest(ITestOutputHelper output)
{
    readonly VerifyHelper verifier = new(output, "CAF");
}
