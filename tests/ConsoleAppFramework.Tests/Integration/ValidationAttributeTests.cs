using System;
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Xunit;
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Global

namespace ConsoleAppFramework.Integration.Test;

public class ValidationAttributeTests
{
	/// <summary>
	/// Try to execute command with invalid option value.
	/// </summary>
	[Fact]
	public void Validate_String_Length_Test()
	{
		using var console = new CaptureConsoleOutput();

		const string optionName = "arg";
		const string optionValue = "too-large-string-value";

		var args = new[] { nameof(AppWithValidationAttributes.StrLength), $"--{optionName}", optionValue };
		Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<AppWithValidationAttributes>(args);

		// Validation fails, so StrLength command is not executed.
		console.Output.Should().NotContain(AppWithValidationAttributes.StrLengthOutput);

		console.Output.Should().Contain(optionName);
		console.Output.Should().Contain(optionValue);
	}

	/// <inheritdoc />
	internal class AppWithValidationAttributes : ConsoleAppBase
	{
		public const string StrLengthOutput = $"hello from {nameof(StrLength)}";

		public void StrLength([StringLength(maximumLength: 8)] string arg) => Console.WriteLine(StrLengthOutput);
	}
}