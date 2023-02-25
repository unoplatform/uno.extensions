using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Uno.Extensions.Core.Generators.Helpers;
using Uno.Extensions.Edition;

namespace Uno.Extensions.Generators.PropertySelector;

internal readonly record struct PropertySelectorCandidate
{
	public PropertySelectorCandidate(GeneratorSyntaxContext context, CancellationToken ct)
	{
		IsValid = true;
		Syntax = (InvocationExpressionSyntax)context.Node;
		Location = Syntax.SyntaxTree.GetLineSpan(Syntax.Span);
		
		var method = context.SemanticModel.GetSymbolInfo(Syntax, ct).Symbol as IMethodSymbol;
		
		if (method is null ||
			!method.Parameters.Any(p => p.Type is INamedTypeSymbol
			{
				IsGenericType: true,
				MetadataName: "PropertySelector`2",
				ContainingNamespace: { Name: "Edition", ContainingNamespace: { Name: "Extensions", ContainingNamespace.Name: "Uno" } }
			}))
		{
			IsValid = false;
			Accessors = null;
			MethodGlobalNamespace = null;
			MethodName = null;
			return;
		}

		MethodName = method.Name;
		MethodGlobalNamespace = method.ContainingModule?.GlobalNamespace?.ToString() ?? "";

		var callerPathParameter = method.Parameters.FirstOrDefault(p => p.HasAttribute<CallerFilePathAttribute>());
		var callerLineParameter = method.Parameters.FirstOrDefault(p => p.HasAttribute<CallerLineNumberAttribute>());

		if (callerPathParameter is null || callerLineParameter is null)
		{
			IsValid = false;
			Accessors = null;
			return;
		}

		var arguments = Syntax.ArgumentList.Arguments;

		var callerPathArg = arguments.ElementAtOrDefault(callerPathParameter.Ordinal)?.Expression;
		var callerPath = callerPathArg switch
		{
			LiteralExpressionSyntax pathArg => pathArg.Token.ValueText,
			// It might be something else, but in this case the analyzer will complain.
			_ => Location.Path,
		};

		var callerLineArg = arguments.ElementAtOrDefault(callerLineParameter.Ordinal)?.Expression;
		var callerLine = callerLineArg switch
		{
			LiteralExpressionSyntax lineArg => lineArg.Token.ValueText,
			// It might be something else, but in this case the analyzer will complain.
			_ => (Location.StartLinePosition.Line + 1).ToString(CultureInfo.InvariantCulture),
		};

		Accessors = method.Parameters
			.Where(param => param.Type is INamedTypeSymbol
			{
				IsGenericType: true,
				MetadataName: "PropertySelector`2",
				ContainingNamespace: { Name: "Edition", ContainingNamespace: { Name: "Extensions", ContainingNamespace.Name: "Uno" } }
			})
			.Select(param =>
				(key: $"{param.Name}{callerPath}{callerLine}", accessor: GenerateAccessor((INamedTypeSymbol)param.Type, arguments.ElementAtOrDefault(param.Ordinal))))
			.Where(a => a.accessor is not null)
			.ToImmutableArray().AsEquatableArray()!;
	}

	[MemberNotNullWhen(true, nameof(Syntax), nameof(Accessors), nameof(MethodGlobalNamespace), nameof(MethodName))]
	public bool IsValid { get; }

	public InvocationExpressionSyntax? Syntax { get; }
	public FileLinePositionSpan Location { get; }
	public string? MethodGlobalNamespace { get; }
	public string? MethodName { get; }

	public EquatableArray<(string key, string accessor)>? Accessors { get; }

	private static string? GenerateAccessor(INamedTypeSymbol selectorType, ArgumentSyntax? selectorArg)
	{
		if (selectorArg?.Expression is not SimpleLambdaExpressionSyntax selector)
		{
			return null;
		}

		if (selector.Parameter is null or { IsMissing: true } or { Identifier.ValueText.Length: <= 0 }
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
		=> $"entity => entity{path.FullPath}"; // Note: this is expected to be the SimpleLambdaExpressionSyntax selector

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
				return "";
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
				return "";
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
