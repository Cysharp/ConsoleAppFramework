using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ConsoleAppFramework.Tests
{
    public class ExitCodeTest
    {
        public class ExitCodeTestBatch : ConsoleAppBase
        {
            [Command(nameof(NoExitCode))]
            public void NoExitCode()
            {
            }

            [Command(nameof(NoExitCodeException))]
            public void NoExitCodeException()
            {
                throw new Exception();
            }

            [Command(nameof(NoExitCodeWithTask))]
            public Task NoExitCodeWithTask()
            {
                return Task.CompletedTask;
            }

            [Command(nameof(ExitCode))]
            public int ExitCode()
            {
                return 12345;
            }

            [Command(nameof(ExitCodeException))]
            public int ExitCodeException()
            {
                throw new Exception();
            }

            [Command(nameof(ExitCodeWithTask))]
            public Task<int> ExitCodeWithTask()
            {
                return Task.FromResult(54321);
            }

            [Command(nameof(ExitCodeWithTaskException))]
            public Task<int> ExitCodeWithTaskException()
            {
                throw new Exception();
            }
        }

        [Fact]
        public async Task NoExitCode()
        {
            Environment.ExitCode = 0;
            await new HostBuilder().RunConsoleAppEngineAsync<ExitCodeTestBatch>(new[] { nameof(NoExitCode) });
            Assert.Equal(0, Environment.ExitCode);
        }

        [Fact]
        public async Task NoExitCodeWithTask()
        {
            Environment.ExitCode = 0;
            await new HostBuilder().RunConsoleAppEngineAsync<ExitCodeTestBatch>(new[] { nameof(NoExitCodeWithTask) });
            Assert.Equal(0, Environment.ExitCode);
        }

        [Fact]
        public async Task NoExitCodeException()
        {
            Environment.ExitCode = 0;
            await new HostBuilder().RunConsoleAppEngineAsync<ExitCodeTestBatch>(new[] { nameof(NoExitCodeException) });
            Assert.Equal(1, Environment.ExitCode);
        }

        [Fact]
        public async Task ExitCode()
        {
            Environment.ExitCode = 0;
            await new HostBuilder().RunConsoleAppEngineAsync<ExitCodeTestBatch>(new[] { nameof(ExitCode) });
            Assert.Equal(12345, Environment.ExitCode);
        }

        [Fact]
        public async Task ExitCodeException()
        {
            Environment.ExitCode = 0;
            await new HostBuilder().RunConsoleAppEngineAsync<ExitCodeTestBatch>(new[] { nameof(ExitCodeException) });
            Assert.Equal(1, Environment.ExitCode);
        }

        [Fact]
        public async Task ExitCodeWithTask()
        {
            Environment.ExitCode = 0;
            await new HostBuilder().RunConsoleAppEngineAsync<ExitCodeTestBatch>(new[] { nameof(ExitCodeWithTask) });
            Assert.Equal(54321, Environment.ExitCode);
        }

        [Fact]
        public async Task ExitCodeWithTaskException()
        {
            Environment.ExitCode = 0;
            await new HostBuilder().RunConsoleAppEngineAsync<ExitCodeTestBatch>(new[] { nameof(ExitCodeWithTaskException) });
            Assert.Equal(1, Environment.ExitCode);
        }

    }
}
