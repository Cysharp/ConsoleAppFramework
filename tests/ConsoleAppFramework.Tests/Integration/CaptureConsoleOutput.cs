using System;
using System.IO;

namespace ConsoleAppFramework.Integration.Test
{
    public class CaptureConsoleOutput : IDisposable
    {
        private readonly TextWriter _originalWriter;
        private readonly StringWriter _stringWriter;

        public CaptureConsoleOutput()
        {
            _originalWriter = Console.Out;
            _stringWriter = new StringWriter();
            Console.SetOut(_stringWriter);
        }

        public string Output => _stringWriter.ToString();

        public void Dispose()
        {
            Console.SetOut(_originalWriter);
        }
    }
}