using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Generators.PropertySelector;

internal class PropertySelectorsGenerationTool : ICodeGenTool
{
	/// <inheritdoc />
	public string Version => "1";

	public void Generate(SourceProductionContext ctx, PropertySelectorCandidate candidate, string? assemblyName)
	{
		try
		{
			if (!candidate.IsValid)
			{
				return;
			}

			var accessors = candidate.Accessors;
			if (accessors.Value.IsEmpty)
			{
				return;
			}

			var id = GetId(candidate, assemblyName);
			
			ctx.AddSource(id, GenerateRegistrationClass(assemblyName, id, candidate, accessors));
		}
		catch (Exception error)
		{
			ctx.ReportDiagnostic(Rules.PS9999.GetDiagnostic(error));
		}
	}

	private string GetId(PropertySelectorCandidate candidate, string? assemblyName)
	{
		var filePath = candidate.Location.Path;
		if (assemblyName is { Length: > 0 })
		{
			var index = filePath.LastIndexOf(assemblyName, StringComparison.OrdinalIgnoreCase);
			if (index > 0)
			{
				filePath = filePath.Substring(index + assemblyName.Length).TrimStart(Path.GetInvalidFileNameChars());
			}
		}

		return Clean(filePath) + "_" + (candidate.Location.StartLinePosition.Line + 1) + "_" + candidate.Location.StartLinePosition.Character;

		static string Clean(string filePath)
		{
			var sb = new StringBuilder(filePath.Length);
			foreach (var c in filePath)
			{
				sb.Append(char.IsLetterOrDigit(c) ? c : '_');
			}

			return sb.ToString();
		}
	}

	private string GenerateRegistrationClass(string? assemblyName, string id, PropertySelectorCandidate usage, IEnumerable<(string key, string accessor)> accessors)
		=> $@"{this.GetFileHeader(3)}

			namespace {assemblyName}.__PropertySelectors
			{{
				/// <summary>
				/// Auto registration class for PropertySelector used in {usage.MethodGlobalNamespace}.
				/// </summary>
				[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
				internal static class {id}
				{{
					/// <summary>
					/// Register the value providers for the PropertySelectors used to invoke '{usage.MethodName}'
					/// in {usage.Location.Path} on line {usage.Location.StartLinePosition.Line + 1}.
					/// </summary>
					/// <remarks>
					/// This method is flagged with the [ModuleInitializerAttribute] which means that it will be invoked by the runtime when the module is being loaded.
					/// You should not have to use it at any time.
					/// </remarks>
					[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
					[global::System.Runtime.CompilerServices.ModuleInitializerAttribute]
					internal static void Register()
					{{
						{accessors.Select(a => $"{NS.Edition}.PropertySelectors.Register(\r\n\t@\"{a.key}\",\r\n\t{a.accessor});").Align(6)}
					}}
				}}
			}}".Align(0);
}
