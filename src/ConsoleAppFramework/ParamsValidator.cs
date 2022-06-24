using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace ConsoleAppFramework
{
	/// <summary>
	/// Validator of command parameters.
	/// </summary>
	public interface IParamsValidator
	{
		/// <summary>
		/// Validate <paramref name="parameters"/> of command based on validation attributes
		/// applied to method's parameters.
		/// </summary>
		ValidationResult? ValidateParameters(IEnumerable<(ParameterInfo Parameter, object? Value)> parameters);
	}

	/// <inheritdoc />
	public class ParamsValidator : IParamsValidator
	{
		private readonly ConsoleAppOptions options;

		public ParamsValidator(ConsoleAppOptions options) => this.options = options;

		/// <inheritdoc />
		ValidationResult? IParamsValidator.ValidateParameters(
			IEnumerable<(ParameterInfo Parameter, object? Value)> parameters)
		{
			var invalidParameters = parameters
				.Select(tuple => (tuple.Parameter, tuple.Value, Result: Validate(tuple.Parameter, tuple.Value)))
				.Where(tuple => tuple.Result != ValidationResult.Success)
				.ToImmutableArray();

			if (!invalidParameters.Any())
			{
				return ValidationResult.Success;
			}

			var errorMessage = string.Join(Environment.NewLine,
				invalidParameters
					.Select(tuple => 
						$"{options.NameConverter(tuple.Parameter.Name!)} " +
						$"({tuple.Value}): " +
						$"{tuple.Result!.ErrorMessage}")
			);

			return new ValidationResult($"Some parameters have invalid values:{Environment.NewLine}{errorMessage}");
		}

		private static ValidationResult? Validate(ParameterInfo parameterInfo, object? value)
		{
			if (value is null) return ValidationResult.Success;

			var validationContext = new ValidationContext(value, null, null);

			var failedResults = GetValidationAttributes(parameterInfo)
				.Select(attribute => attribute.GetValidationResult(value, validationContext))
				.Where(result => result != ValidationResult.Success)
				.ToImmutableArray();

			return failedResults.Any()
				? new ValidationResult(string.Join("; ", failedResults.Select(res => res?.ErrorMessage)))
				: ValidationResult.Success;
		}

		private static IEnumerable<ValidationAttribute> GetValidationAttributes(ParameterInfo parameterInfo)
			=> parameterInfo
				.GetCustomAttributes()
				.OfType<ValidationAttribute>();
	}
}
