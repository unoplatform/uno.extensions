using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Generators;

internal static class GeneratorExecutionContextExtensions
{
	private const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";

	public static string GetMSBuildPropertyValue(
		this GeneratorExecutionContext context,
		string name,
		string defaultValue = "")
	{
		context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.{name}", out var value);
		return value ?? defaultValue;
	}

	public static bool TryGetOptionValue(this GeneratorExecutionContext context, AdditionalText textFile, string key, [NotNullWhen(true)] out string? value)
	{
		return context.AnalyzerConfigOptions.GetOptions(textFile).TryGetValue(key, out value);
	}

	public static bool IsDesignTime(this GeneratorExecutionContext context)
	{
		var isBuildingProjectValue = context.GetMSBuildPropertyValue("BuildingProject"); // legacy projects
		var isDesignTimeBuildValue = context.GetMSBuildPropertyValue("DesignTimeBuild"); // sdk-style projects

		return string.Equals(isBuildingProjectValue, "false", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(isDesignTimeBuildValue, "true", StringComparison.OrdinalIgnoreCase);
	}
}
