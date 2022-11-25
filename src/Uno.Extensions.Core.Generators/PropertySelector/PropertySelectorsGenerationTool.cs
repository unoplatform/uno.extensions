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

	public void Generate(SourceProductionContext ctx, PropertySelectorCandidate candidate)
	{
		try
		{
			var usage = PropertySelectorUsageResolver.FindUsage(candidate);
			if (usage is null)
			{
				return;
			}

			var accessors = usage
				.Value
				.Items
				.Select(item => (item.Key, code: GenerateAccessor(item.Parameter.Type, item.Argument)!))
				.Where(item => item.code is not null)
				.ToList();
			if (accessors is { Count: 0 })
			{
				return;
			}

			var assembly = candidate.Context.SemanticModel.Compilation.Assembly;
			var id = GetId(candidate);
			
			ctx.AddSource(id, GenerateRegistrationClass(assembly, id, usage.Value, accessors));
		}
		catch (GenerationException genError)
		{
			foreach (var diag in genError.Diagnostics)
			{
				ctx.ReportDiagnostic(diag);
			}
		}
		catch (Exception error)
		{
			ctx.ReportDiagnostic(Rules.PS9999.GetDiagnostic(error));
		}
	}

	private string GetId(PropertySelectorCandidate candidate)
	{
		var filePath = candidate.Location.Path;
		if (candidate.Context.SemanticModel.Compilation.AssemblyName is { Length: > 0 } assemblyName)
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

	private string GenerateRegistrationClass(IAssemblySymbol assembly, string id, PropertySelectorUsage usage, IEnumerable<(string key, string accessor)> accessors)
		=> $@"{this.GetFileHeader(3)}

			namespace {assembly.Name}.__PropertySelectors
			{{
				/// <summary>
				/// Auto registration class for PropertySelector used in {usage.Method.ContainingModule.GlobalNamespace}.
				/// </summary>
				[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
				internal static class {id}
				{{
					/// <summary>
					/// Register the value providers for the PropertySelectors used to invoke '{usage.Method.Name}'
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

	private string? GenerateAccessor(ITypeSymbol selectorType, ArgumentSyntax? selectorArg)
	{
		if (selectorArg is null)
		{
			return null; // Cannot generate yet
		}

		if (selectorType is not INamedTypeSymbol { TypeArguments.Length: 2 } type)
		{
			throw new InvalidOperationException("Invalid type, at this point the expected type is a bounded PropertySelector<TEntity, TProperty>.");
		}

		var entityType = type.TypeArguments[0];
		var propertyType = type.TypeArguments[1];
		if (entityType is null or { Kind: SymbolKind.ErrorType }
			|| propertyType is null or { Kind: SymbolKind.ErrorType })
		{
			// Failed to properly resolve the types, we cannot generate yet
			return null;
		}

		if (entityType is not INamedTypeSymbol { IsRecord: true })
		{
			throw Rules.PS0004.Fail(selectorArg, entityType);
		}

		if (selectorArg.Expression is null or { IsMissing: true })
		{
			// The argument has not been defined yet, we cannot generate.
			return null;
		}

		if (selectorArg.Expression is not SimpleLambdaExpressionSyntax selector)
		{
			throw Rules.PS0003.Fail(selectorArg);
		}

		if (selector.Parameter is null or { IsMissing: true } or { Identifier.ValueText.Length: <= 0 }
			|| selector.Body is null or { IsMissing: true })
		{
			// Delegate is not defined properly yet, we cannot generate.
			return null;
		}

		var path = PropertySelectorPathResolver.Resolve(selector);
		var valueAccessor = $@"new {NS.Edition}.ValueAccessor<{entityType}, {propertyType}>(
			""{path.FullPath}"",
			{GenerateGetter(entityType, path).Align(3)},
			{GenerateSetter(entityType, path).Align(3)})";

		return valueAccessor.Align(0);
	}

	private string GenerateGetter(ITypeSymbol record, PropertySelectorPath path)
		=> $"entity => entity{path.FullPath}"; // Note: this is expected to be the SimpleLambdaExpressionSyntax selector

	private string GenerateSetter(ITypeSymbol record, PropertySelectorPath path)
	{
		if (path.Parts.Count is 1)
		{
			var part = path.Parts[0];
			var current = OrDefault(path, part, record) is { Length: > 0 } orDefault
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

					current.Add($"var current_{i} = current_{i - 1}{part.Accessor}{OrDefault(path, part, type)};");
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

		static string OrDefault(PropertySelectorPath path, PropertySelectorPathPart part, ITypeSymbol type)
		{
			if (type is { NullableAnnotation: NullableAnnotation.NotAnnotated })
			{
				// The type is annotated to NOT be nullable, we don't try to create a default instance.
				return "";
			}

			if (type is not INamedTypeSymbol { IsRecord: true } record)
			{
				throw Rules.PS0005.Fail(path.FullPath, part.Name, part.Node, type);
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
				throw Rules.PS0006.Fail(path.FullPath, part.Name, part.Node, type);
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
