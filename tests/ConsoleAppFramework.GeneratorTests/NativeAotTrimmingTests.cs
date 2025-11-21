using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ConsoleAppFramework.GeneratorTests;

public class NativeAotTrimmingTests
{
    [Test]
    public async Task NativeAotTrimmingSample_PublishesAndRuns()
    {
        var publishDir = Directory.CreateTempSubdirectory("caf-nativeaot").FullName;

        var currentDir = AppContext.BaseDirectory;
        var testsDir = $"tests{Path.DirectorySeparatorChar}";
        var root = currentDir[..(currentDir.IndexOf(testsDir) + testsDir.Length)];

        var publish = StartProcess("dotnet", [
            "publish",
            Path.Combine(root, "NativeAotTrimming", "NativeAotTrimming.csproj"),
            "-c", "Release",
            "-o", publishDir
        ]);
        publish.WaitForExit();
        var publishStdOut = publish.StandardOutput.ReadToEnd();
        var publishStdErr = publish.StandardError.ReadToEnd();

        await Assert.That(publish.ExitCode).IsZero().Because($"dotnet publish failed:{Environment.NewLine}{publishStdOut}{publishStdErr}");

        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "NativeAotTrimming.exe"
            : "NativeAotTrimming";
        var exePath = Path.Combine(publishDir, exeName);

        await Assert.That(File.Exists(exePath)).IsTrue().Because($"Expected published executable at {exePath}");

        var app = StartProcess(exePath, [], workingDirectory: publishDir);
        app.WaitForExit();
        var appStdOut = app.StandardOutput.ReadToEnd();
        var appStdErr = app.StandardError.ReadToEnd();

        await Assert.That(app.ExitCode).IsZero().Because($"App should execute successfully after AOT trimming:{Environment.NewLine}stdout:{appStdOut}{Environment.NewLine}stderr:{appStdErr}");
    }

    private static Process StartProcess(string fileName, IReadOnlyCollection<string> arguments, string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
        };

        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        return Process.Start(psi)!;
    }
}
