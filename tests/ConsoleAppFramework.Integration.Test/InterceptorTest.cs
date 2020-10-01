//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using FluentAssertions;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Xunit;

//// ReSharper disable InconsistentNaming

//namespace ConsoleAppFramework.Integration.Test
//{
//    public partial class InterceptorTest
//    {
//        [Fact]
//        public void Single()
//        {
//            using var console = new CaptureConsoleOutput();
//            var args = new[] { "Cysharp" };
//            var interceptor = new TestInterceptor();
//            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<InterceptorTest_Single>(args, interceptor);

//            interceptor.Outputs.Should().Equal("OnEngineBeginAsync", "OnMethodBeginAsync", "OnMethodEndAsync", "OnEngineCompleteAsync");
//            console.Output.Should().Contain("Hello Cysharp");
//        }

//        //[Fact]
//        //public void Single_Insufficient_Arguments()
//        //{
//        //    using var console = new CaptureConsoleOutput();
//        //    var args = new string[] { };
//        //    var interceptor = new TestInterceptor();
//        //    Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<InterceptorTest_Single>(args, interceptor);

//        //    interceptor.Outputs.Should().Equal("OnEngineBeginAsync", "OnMethodBeginAsync", "OnMethodEndAsync", "OnEngineCompleteAsync");
//        //    console.Output.Should().Contain("Usage");
//        //}

//        [Fact]
//        public void Multi()
//        {
//            using var console = new CaptureConsoleOutput();
//            var args = new[] { "hello", "Cysharp" };
//            var interceptor = new TestInterceptor();
//            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<InterceptorTest_Multi>(args, interceptor);

//            interceptor.Outputs.Should().Equal("OnEngineBeginAsync", "OnMethodBeginAsync", "OnMethodEndAsync", "OnEngineCompleteAsync");
//            console.Output.Should().Contain("Hello Cysharp");
//        }

//        [Fact]
//        public void Multi_Insufficient_Arguments()
//        {
//            using var console = new CaptureConsoleOutput();
//            var args = new[] { "Cysharp" };
//            var interceptor = new TestInterceptor();
//            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<InterceptorTest_Multi>(args, interceptor);

//            interceptor.Outputs.Should().Equal("OnEngineBeginAsync", "OnMethodBeginAsync", "OnMethodEndAsync", "OnEngineCompleteAsync");
//            console.Output.Should().Contain("Usage");
//        }

//        public class TestInterceptor : IConsoleAppInterceptor
//        {
//            public List<string> Outputs { get; } = new List<string>();

//            public ValueTask OnEngineBeginAsync(IServiceProvider serviceProvider, ILogger<ConsoleAppEngine> logger)
//            {
//                Outputs.Add("OnEngineBeginAsync");
//                return default;
//            }

//            public ValueTask OnMethodBeginAsync(ConsoleAppContext context)
//            {
//                Outputs.Add("OnMethodBeginAsync");
//                return default;
//            }

//            public ValueTask OnMethodEndAsync(ConsoleAppContext context, string? errorMessageIfFailed, Exception? exceptionIfExists)
//            {
//                Outputs.Add("OnMethodEndAsync");
//                return default;
//            }

//            public ValueTask OnEngineCompleteAsync(IServiceProvider serviceProvider, ILogger<ConsoleAppEngine> logger)
//            {
//                Outputs.Add("OnEngineCompleteAsync");
//                return default;
//            }
//        }

//        public class InterceptorTest_Single : ConsoleAppBase
//        {
//            public void Hello([Option(0)]string name) => Console.WriteLine($"Hello {name}");
//        }

//        public class InterceptorTest_Multi : ConsoleAppBase
//        {
//            [Command("hello")]
//            public void Hello([Option(0)]string name) => Console.WriteLine($"Hello {name}");
//            [Command("hello2")]
//            public void Hello2([Option(0)]string name) => Console.WriteLine($"Hello {name}");
//        }
//    }
//}
