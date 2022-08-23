using System;
using System.Linq;
using System.Reflection;

namespace ConsoleAppFramework
{
	public static class ParameterInfoExtensions
	{
		public static bool HasDefaultValue(this ParameterInfo pi)
			=> pi.HasDefaultValue
			|| pi.CustomAttributes.Any(a => a.AttributeType == typeof(ParamArrayAttribute));

		public static object? DefaultValue(this ParameterInfo pi)
			=> pi.HasDefaultValue
			? pi.DefaultValue
			: Array.CreateInstance(pi.ParameterType.GetElementType()!, 0);
	}
}
