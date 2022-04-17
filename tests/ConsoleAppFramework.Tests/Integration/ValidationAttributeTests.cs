using System;
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
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
		ConsoleApp.Run<AppWithValidationAttributes>(args);

		// Validation should fail, so StrLength command should not be executed.
		console.Output.Should().NotContain(AppWithValidationAttributes.Output);

		console.Output.Should().Contain(optionName);
		console.Output.Should().Contain(optionValue);
	}

	[Fact]
	public void Command_With_Multiple_Params()
	{
		using var console = new CaptureConsoleOutput();

		var args = new[]
		{
			nameof(AppWithValidationAttributes.MultipleParams),
			"--second-arg", "10",
			"--first-arg", "invalid-email-address"
		};

		ConsoleApp.Run<AppWithValidationAttributes>(args);

		// Validation should fail, so StrLength command should not be executed.
		console.Output.Should().NotContain(AppWithValidationAttributes.Output);
	}

	/// <inheritdoc />
	internal class AppWithValidationAttributes : ConsoleAppBase
	{
		public const string Output = $"hello from {nameof(AppWithValidationAttributes)}";

		[Command(nameof(StrLength))]
		public void StrLength([StringLength(maximumLength: 8)] string arg) => Console.WriteLine(Output);

		[Command(nameof(MultipleParams))]
		public void MultipleParams(
			[EmailAddress] string firstArg,
			[Range(0, 2)]  int secondArg) => Console.WriteLine(Output);
	}
}