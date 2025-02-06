using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppFramework.GeneratorTests;

public class IncrementalGeneratorTest
{
    void VerifySourceOutputReasonIsCached((string Key, string Reasons)[] reasons)
    {
        var reason = reasons.FirstOrDefault(x => x.Key == "SourceOutput").Reasons;
        reason.ShouldBe("Cached");
    }

    void VerifySourceOutputReasonIsNotCached((string Key, string Reasons)[] reasons)
    {
        var reason = reasons.FirstOrDefault(x => x.Key == "SourceOutput").Reasons;
        reason.ShouldNotBe("Cached");
    }

    [Fact]
    public void RunLambda()
    {
        var step1 = """
using ConsoleAppFramework;

ConsoleApp.Run(args, int () => 0);
""";

        var step2 = """
using ConsoleAppFramework;

ConsoleApp.Run(args, int () => 100); // body change

Console.WriteLine("foo"); // unrelated line
""";

        var step3 = """
using ConsoleAppFramework;

ConsoleApp.Run(args, int (int x, int y) => 99); // change signature

Console.WriteLine("foo"); // unrelated line
""";

        var reasons = CSharpGeneratorRunner.GetIncrementalGeneratorTrackedStepsReasons("ConsoleApp.Run.", step1, step2, step3);

        reasons[0][0].Reasons.ShouldBe("New");
        reasons[1][0].Reasons.ShouldBe("Unchanged");
        reasons[2][0].Reasons.ShouldBe("Modified");

        VerifySourceOutputReasonIsCached(reasons[1]);
        VerifySourceOutputReasonIsNotCached(reasons[2]);
    }

    [Fact]
    public void RunMethod()
    {
        var step1 = """
using ConsoleAppFramework;

var tako = new Tako();
ConsoleApp.Run(args, tako.DoHello);

public class Tako
{
    /// <summary>
    /// AAAAA
    /// </summary>
    public void DoHello()
    {
    }
}
""";

        var step2 = """
using ConsoleAppFramework;

var tako = new Tako();
ConsoleApp.Run(args, tako.DoHello);

Console.WriteLine("foo"); // unrelated line

public class Tako
{
    /// <summary>
    /// AAAAA
    /// </summary>
    public void DoHello()
    {
        Console.WriteLine("foo"); // body change
    }
}
""";

        var step3 = """
using ConsoleAppFramework;

var tako = new Tako();
ConsoleApp.Run(args, tako.DoHello);

public class Tako
{
    /// <summary>
    /// AAAAA
    /// </summary>
    public void DoHello(int x, int y) // signature change
    {
    }
}
""";

        var reasons = CSharpGeneratorRunner.GetIncrementalGeneratorTrackedStepsReasons("ConsoleApp.Run.", step1, step2, step3);

        reasons[0][0].Reasons.ShouldBe("New");
        reasons[1][0].Reasons.ShouldBe("Unchanged");
        reasons[2][0].Reasons.ShouldBe("Modified");

        VerifySourceOutputReasonIsCached(reasons[1]);
    }

    [Fact]
    public void RunMethodRef()
    {
        var step1 = """
using ConsoleAppFramework;

unsafe
{
    ConsoleApp.Run(args, &DoHello);
}

static void DoHello()
{
}
""";

        var step2 = """
using ConsoleAppFramework;

unsafe
{
    ConsoleApp.Run(args, &DoHello);
    Console.WriteLine("bar"); // unrelated line
}

static void DoHello()
{
    Console.WriteLine("foo"); // body 
}
""";

        var step3 = """
using ConsoleAppFramework;

unsafe
{
    ConsoleApp.Run(args, &DoHello);
    Console.WriteLine("bar");
}

static void DoHello(int x, int y) // change signature
{
    Console.WriteLine("foo");
}
""";

        var reasons = CSharpGeneratorRunner.GetIncrementalGeneratorTrackedStepsReasons("ConsoleApp.Run.", step1, step2, step3);

        reasons[0][0].Reasons.ShouldBe("New");
        reasons[1][0].Reasons.ShouldBe("Unchanged");
        reasons[2][0].Reasons.ShouldBe("Modified");

        VerifySourceOutputReasonIsCached(reasons[1]);
    }

    [Fact]
    public void Builder()
    {
        var step1 = """
using Foo.Bar;

var app = ConsoleApp.Create();
app.Add<Test>();
app.Add("", () => { });
app.UseFilter<MyFilter>();
app.Run(args);

var l = new List<int>();
l.Add(10);

public class Test
{
    public void Show(string aaa, [Range(0, 1)] double value) => ConsoleApp.Log($"{value}");
}

namespace Foo.Bar
{
    public class MyFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
    {
        public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public class MyFilter2(ConsoleAppFilter next) : ConsoleAppFilter(next)
    {
        public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
""";

        var step2 = """
using Foo.Bar;

var app = ConsoleApp.Create();
app.Add<Test>();
app.Add("", () => 
{
    Console.WriteLine("body"); // body
});
app.UseFilter<MyFilter>();
app.Run(args);

var l = new List<int>();
l.Add(10);

Console.WriteLine(""); // unrelated line

public class Test
{
    public void Show(string aaa, [Range(0, 1)] double value) => ConsoleApp.Log($"{value}");
}

namespace Foo.Bar
{
    public class MyFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
    {
        public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public class MyFilter2(ConsoleAppFilter next) : ConsoleAppFilter(next)
    {
        public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
""";

        var step3 = """
using Foo.Bar;

var app = ConsoleApp.Create();
app.Add<Test>();
app.Add("", (int x, int y) => // different
{
    Console.WriteLine("body"); // body
});
app.UseFilter<MyFilter>();
app.Run(args);

var l = new List<int>();
l.Add(10);

Console.WriteLine(""); // unrelated line

public class Test
{
    public void Show(string aaa, [Range(0, 1)] double value) => ConsoleApp.Log($"{value}");
}

namespace Foo.Bar
{
    public class MyFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
    {
        public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public class MyFilter2(ConsoleAppFilter next) : ConsoleAppFilter(next)
    {
        public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
""";

        var step4 = """
using Foo.Bar;

var app = ConsoleApp.Create();
app.Add<Test>();
app.Add("", (int x, int y) => // different
{
    Console.WriteLine("body"); // body
});
app.Add("foo", () => {}); // newline
app.UseFilter<MyFilter>();
app.Run(args);

var l = new List<int>();
l.Add(10);

Console.WriteLine(""); // unrelated line

public class Test
{
    public void Show(string aaa, [Range(0, 1)] double value) => ConsoleApp.Log($"{value}");
}

namespace Foo.Bar
{
    public class MyFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
    {
        public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public class MyFilter2(ConsoleAppFilter next) : ConsoleAppFilter(next)
    {
        public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
""";

        var reasons = CSharpGeneratorRunner.GetIncrementalGeneratorTrackedStepsReasons("ConsoleApp.Builder.", step1, step2, step3, step4);

        VerifySourceOutputReasonIsNotCached(reasons[0]);
        VerifySourceOutputReasonIsCached(reasons[1]);
        VerifySourceOutputReasonIsNotCached(reasons[2]);
        VerifySourceOutputReasonIsNotCached(reasons[3]);
    }

    [Fact]
    public void BuilderOtherType()
    {
        var step1 = """
var app = ConsoleApp.Create();
app.Add("", () => { });
app.Run(args);
""";

        var step2 = """
var l = new List<int>();
l.Add(10);

var app = ConsoleApp.Create();
app.Add("", () => { });
app.Run(args);
""";

        var reasons = CSharpGeneratorRunner.GetIncrementalGeneratorTrackedStepsReasons("ConsoleApp.Builder.", step1, step2);

        VerifySourceOutputReasonIsNotCached(reasons[0]);
        VerifySourceOutputReasonIsCached(reasons[1]);
    }

    [Fact]
    public void BuilderClassChange()
    {
        var step1 = """
var app = ConsoleApp.Create();
app.Add<Test>();
app.Run(args);

public class Test
{
    public void Show(string aaa, [Range(0, 1)] double value) => ConsoleApp.Log($"{value}");
}
""";

        var step2 = """
var app = ConsoleApp.Create();
app.Add<Test>();
app.Run(args);

public class Test
{
    public void Show(string aaa, [Range(0, 1)] double value, int x2) => ConsoleApp.Log($"{value}"); // new parameter
}
""";

        var step3 = """
var app = ConsoleApp.Create();
app.Add<Test>();
app.Run(args);

public class Test
{
    public void Show(string aaa, [Range(0, 1)] double value, int x2) => ConsoleApp.Log($"{value} aiueo"); // body change

    void PrivateMethod()
    {
    }
}
""";

        var step4 = """
var app = ConsoleApp.Create();

var l = new List<int>(); // noise
l.Add(10);

app.Add<Test>();
app.Run(args);

public class Test
{
    public void Show(string aaa, [Range(0, 1)] double value, int x2) => ConsoleApp.Log($"{value} aiueo");

    void PrivateMethod()
    {
    }
}
""";

        var reasons = CSharpGeneratorRunner.GetIncrementalGeneratorTrackedStepsReasons("ConsoleApp.Builder.", step1, step2, step3, step4);

        VerifySourceOutputReasonIsNotCached(reasons[0]);
        VerifySourceOutputReasonIsNotCached(reasons[1]);
        VerifySourceOutputReasonIsCached(reasons[2]);
        VerifySourceOutputReasonIsCached(reasons[3]);
    }

    [Fact]
    public void BuilderClassInterface()
    {
        var step1 = """
var app = ConsoleApp.Create();
app.Add<Test>();
app.Run(args);

public class Test
{
    public void Show(string aaa, [Range(0, 1)] double value) => ConsoleApp.Log($"{value}");
}
""";

        var step2 = """
var app = ConsoleApp.Create();
app.Add<Test>();
app.Run(args);

public class Test : IDisposable
{
    public void Show(string aaa, [Range(0, 1)] double value, int x2) => ConsoleApp.Log($"{value}");

    public void Dispose() { }
}
""";

        var step3 = """
var app = ConsoleApp.Create();
app.Add<Test>();
app.Run(args);

public class Test : IDisposable, IAsyncDisposable
{
    public void Show(string aaa, [Range(0, 1)] double value, int x2) => ConsoleApp.Log($"{value}");

    public void Dispose() { }
    public ValueTask DisposeAsync() { return default; }
}
""";

        var reasons = CSharpGeneratorRunner.GetIncrementalGeneratorTrackedStepsReasons("ConsoleApp.Builder.", step1, step2, step3);

        VerifySourceOutputReasonIsNotCached(reasons[0]);
        VerifySourceOutputReasonIsNotCached(reasons[1]);
        VerifySourceOutputReasonIsNotCached(reasons[2]);
    }

    [Fact]
    public void ConstructorChange()
    {
        var step1 = """
var app = ConsoleApp.Create();
app.Add<Test>();
app.Run(args);

public class Test
{
    public void Show(string aaa, [Range(0, 1)] double value) => ConsoleApp.Log($"{value}");
}
""";

        var step2 = """
var app = ConsoleApp.Create();
app.Add<Test>();
app.Run(args);

public class Test
{
    public Test(IDisposable d)
    {
    }

    public void Show(string aaa, [Range(0, 1)] double value, int x2) => ConsoleApp.Log($"{value}");
}
""";

        var step3 = """
var app = ConsoleApp.Create();
app.Add<Test>();
app.Run(args);

public class Test
{
    public Test(IAsyncDisposable d)
    {
    }

    public void Show(string aaa, [Range(0, 1)] double value, int x2) => ConsoleApp.Log($"{value}");
}
""";

        // same as step3
        var step4 = """
var app = ConsoleApp.Create();
app.Add<Test>();
app.Run(args);

public class Test
{
    public Test(IAsyncDisposable d)
    {
    }

    public void Show(string aaa, [Range(0, 1)] double value, int x2) => ConsoleApp.Log($"{value}");
}
""";

        var reasons = CSharpGeneratorRunner.GetIncrementalGeneratorTrackedStepsReasons("ConsoleApp.Builder.", step1, step2, step3, step4);

        VerifySourceOutputReasonIsNotCached(reasons[0]);
        VerifySourceOutputReasonIsNotCached(reasons[1]);
        VerifySourceOutputReasonIsNotCached(reasons[2]);
        VerifySourceOutputReasonIsCached(reasons[3]);
    }

    [Fact]
    public void PrimaryConstructorChange()
    {
        var step1 = """
var app = ConsoleApp.Create();
app.Add<Test>();
app.Run(args);

public class Test
{
    public void Show(string aaa, [Range(0, 1)] double value) => ConsoleApp.Log($"{value}");
}
""";

        var step2 = """
var app = ConsoleApp.Create();
app.Add<Test>();
app.Run(args);

public class Test(IDisposable d)
{
    public void Show(string aaa, [Range(0, 1)] double value, int x2) => ConsoleApp.Log($"{value}");
}
""";

        var step3 = """
var app = ConsoleApp.Create();
app.Add<Test>();
app.Run(args);

public class Test(IAsyncDisposable d)
{
    public void Show(string aaa, [Range(0, 1)] double value, int x2) => ConsoleApp.Log($"{value}");
}
""";
        var step4 = """
var app = ConsoleApp.Create();
app.Add<Test>();
app.Run(args);

public class Test(IAsyncDisposable d)
{
    public void Show(string aaa, [Range(0, 1)] double value, int x2) => ConsoleApp.Log($"{value}");
}
""";

        var reasons = CSharpGeneratorRunner.GetIncrementalGeneratorTrackedStepsReasons("ConsoleApp.Builder.", step1, step2, step3, step4);

        VerifySourceOutputReasonIsNotCached(reasons[0]);
        VerifySourceOutputReasonIsNotCached(reasons[1]);
        VerifySourceOutputReasonIsNotCached(reasons[2]);
        VerifySourceOutputReasonIsCached(reasons[3]);
    }

    [Fact]
    public void InvalidDefinition()
    {
        var step1 = """
using ConsoleAppFramework;

var app = ConsoleApp.Create();

app.Add("foo", () => { });
app.Add("fooa", () => { });



app.Add("Y", () => { });

app.Run(args);

""";

        // add foo before Y
        var step2 = """
using ConsoleAppFramework;

var app = ConsoleApp.Create();

app.Add("foo", () => { });
app.Add("fooa", () => { });


app.Add("foo", () => { });

app.Add("Y", () => { });

app.Run(args);

""";

        var reasons = CSharpGeneratorRunner.GetIncrementalGeneratorTrackedStepsReasons("ConsoleApp.Builder.", step1, step2);

    }

    [Fact]
    public void IncrDual()
    {
        var step1 = """
using ConsoleAppFramework;

var app = ConsoleApp.Create();
app.Add("aaa", () =>{ });
app.Run(args);
""";

        var step2 = """
using ConsoleAppFramework;

var app = ConsoleApp.Create();
app.Add("aaa", () =>{ });
app.Add("aaa", () =>{ });
app.Run(args);
""";

        var reasons = CSharpGeneratorRunner.GetIncrementalGeneratorTrackedStepsReasons("ConsoleApp.Builder.", step1, step2);
    }
}
