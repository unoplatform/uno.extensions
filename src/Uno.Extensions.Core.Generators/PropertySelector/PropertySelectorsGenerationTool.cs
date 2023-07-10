using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Uno.Extensions.Edition;

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

	internal static string? GenerateAccessor(INamedTypeSymbol selectorType, ArgumentSyntax? selectorArg)
	{
		if (selectorArg?.Expression is not ParenthesizedLambdaExpressionSyntax selector)
		{
			return null;
		}

		if (selector.ParameterList.Parameters.Count != 1 ||
			selector.ParameterList.Parameters[0] is { IsMissing: true } or { Identifier.ValueText.Length: <= 0 }
			|| selector.Body is null or { IsMissing: true })
		{
			// Delegate is not defined properly yet, we cannot generate.
			return null;
		}

		var entityType = selectorType.TypeArguments[0];
		var propertyType = selectorType.TypeArguments[1];

		var path = PropertySelectorPathResolver.Resolve(selector);
		var valueAccessor = $@"new {NS.Edition}.ValueAccessor<{entityType}, {propertyType}>(
			""{path.FullPath}"",
			{GenerateGetter(path).Align(3)},
			{GenerateSetter(entityType, path).Align(3)})";

		return valueAccessor.Align(0);
	}

	private static string GenerateGetter(PropertySelectorPath path)
		=> $"entity => entity{path.FullPath}"; // Note: this is expected to be the ParenthesizedLambdaExpressionSyntax selector

	private static string GenerateSetter(ITypeSymbol record, PropertySelectorPath path)
	{
		if (path.Parts.Count is 1)
		{
			var part = path.Parts[0];
			var current = OrDefault(record) is { Length: > 0 } orDefault
				? $"(current {orDefault})"
				: "current";

			return $"(current, updated_{part.Name}) => {current} with {{ {part.Name} = updated_{part.Name} }}";
		}
		else
		{
			var count = path.Parts.Count;
			var type = record;
			var current = new List<string>(count);
			var updated = new List<string>(count);

			for (var i = 1; i <= count; i++)
			{
				var part = path.Parts[i - 1];

				if (i < count) // 'current_x' is a parameter of the delegate for the leaf element.
				{
					type = (type as INamedTypeSymbol)?.FindProperty(part.Name)?.Type;
					if (type is null)
					{
						// The syntax is not valid (trying to get a property which does not exists), we cannot generate yet.
						// But as anyway the compilation will fail, instead of failing the generator we just generate an exception.
						return $"(_, __) => throw new InvalidOperationException(\"Cannot resolve property '{part.Name}' on '{type}'\")";
					}

					current.Add($"var current_{i} = current_{i - 1}{part.Accessor}{OrDefault(type)};");
				}

				updated.Add($"var updated_{i - 1} = current_{i - 1} with {{ {part.Name} = updated_{i} }};");
			}

			updated.Reverse();
			return $@"(current_0, updated_{count}) =>
			{{
				{current.Align(4)}
				{updated.Align(4)}

				return updated_0;
			}}".Align(0);
		}

		static string OrDefault(ITypeSymbol type)
		{
			if (type is { NullableAnnotation: NullableAnnotation.NotAnnotated })
			{
				// The type is annotated to NOT be nullable, we don't try to create a default instance.
				return "";
			}

			if (type is not INamedTypeSymbol { IsRecord: true } record)
			{
				return "/* Rule PS005 failed. */";
			}

			var ctor = record
				.Constructors
				.Where(ctor => ctor.IsAccessible() && !ctor.IsCloneCtor(record))
				.OrderBy(ctor => ctor.HasAttribute<DefaultConstructorAttribute>() ? 0 : 1)
				.ThenBy(ctor => ctor.Parameters.Length)
				.Select(ctor => ctor.Parameters.Select(GetDefault).ToList())
				.FirstOrDefault(ctorArgs => ctorArgs.All(arg => arg.hasDefault));

			if (ctor is null)
			{
				return "/* Rule PS006 failed. */";
			}

			return $" ?? new({ctor.Where(arg => arg.value is not null).Select(arg => arg.value).JoinBy(", ")})";
		}

		static (bool hasDefault, string? value) GetDefault(IParameterSymbol param)
			=> param switch
			{
				{ HasExplicitDefaultValue: true } => (true, null),
				{ Type.NullableAnnotation: NullableAnnotation.Annotated } => (true, $"default({param.Type})"),
				{ } p when p.Type.IsNullable() => (true, $"default({param.Type})"),
				_ => (false, null)
			};
	}
}
